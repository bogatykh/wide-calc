using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrintMeter.Core;
using PrintMeter.Core.Models;

namespace PrintMeter.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly IFormatRegistry _formatRegistry;
    private readonly BatchPdfAnalyzer _batchPdfAnalyzer;
    private readonly IBatchReportWriter _reportWriter;
    private readonly IFileDialogService _fileDialogs;
    private readonly IOptions<PrintMeterOptions> _options;
    private readonly ILogger<MainViewModel> _logger;
    private readonly bool _autoAnalyzeAfterFileSelection;
    private CancellationTokenSource? _cts;
    private readonly List<string> _selectedFiles = [];
    private string? _lastFolderPath;

    public MainViewModel(
        IFormatRegistry formatRegistry,
        BatchPdfAnalyzer batchPdfAnalyzer,
        IBatchReportWriter reportWriter,
        IFileDialogService fileDialogs,
        IOptions<PrintMeterOptions> options,
        ILogger<MainViewModel> logger,
        bool autoAnalyzeAfterFileSelection = true)
    {
        _formatRegistry = formatRegistry;
        _batchPdfAnalyzer = batchPdfAnalyzer;
        _reportWriter = reportWriter;
        _fileDialogs = fileDialogs;
        _options = options;
        _logger = logger;
        _autoAnalyzeAfterFileSelection = autoAnalyzeAfterFileSelection;

        var enabled = _formatRegistry.EnabledFormats;
        _formatA4 = enabled.Contains("A4");
        _formatA3 = enabled.Contains("A3");
        _formatA2 = enabled.Contains("A2");
        _formatA1 = enabled.Contains("A1");
        _formatA1Plus = enabled.Contains("A1+");
        _formatA0 = enabled.Contains("A0");
        _formatA0Plus = enabled.Contains("A0+");
    }

    public ObservableCollection<FileReportRowViewModel> Rows { get; } = new();

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private double _progressValue;

    [ObservableProperty]
    private string _statusText =
        "Добавьте PDF через «Выбрать PDF» или папку. Новый выбор заменяет список и запускает расчёт заново.";

    [ObservableProperty]
    private double _totalLengthMeters;

    [ObservableProperty]
    private string _summaryByFormat = string.Empty;

    [ObservableProperty]
    private bool _recursiveFolders = true;

    [ObservableProperty]
    private bool _utf8BomForCsv = true;

    [ObservableProperty]
    private bool _formatA4 = true;
    [ObservableProperty]
    private bool _formatA3 = true;
    [ObservableProperty]
    private bool _formatA2 = true;
    [ObservableProperty]
    private bool _formatA1 = true;
    [ObservableProperty]
    private bool _formatA1Plus = true;
    [ObservableProperty]
    private bool _formatA0 = true;
    [ObservableProperty]
    private bool _formatA0Plus = true;

    [ObservableProperty]
    private string _formatStatA4 = "—";

    [ObservableProperty]
    private string _formatStatA3 = "—";

    [ObservableProperty]
    private string _formatStatA2 = "—";

    [ObservableProperty]
    private string _formatStatA1 = "—";

    [ObservableProperty]
    private string _formatStatA1Plus = "—";

    [ObservableProperty]
    private string _formatStatA0 = "—";

    [ObservableProperty]
    private string _formatStatA0Plus = "—";

    private BatchReport? _lastReport;

    [RelayCommand]
    private async Task PickFilesAsync()
    {
        var picked = await _fileDialogs.PickPdfFilesAsync().ConfigureAwait(true);
        if (picked is null || picked.Count == 0)
        {
            return;
        }

        _lastFolderPath = null;
        ReplaceSelectedPdfFiles([..picked]);
        AnalyzeCommand.NotifyCanExecuteChanged();
        if (_autoAnalyzeAfterFileSelection)
        {
            StatusText = $"Файлов: {_selectedFiles.Count}. Запуск расчёта…";
            await AnalyzeAsync().ConfigureAwait(true);
            return;
        }

        StatusText =
            $"Выбрано файлов: {_selectedFiles.Count}. Нажмите «Считать», чтобы посчитать метраж (новый выбор заменяет список).";
    }

    [RelayCommand]
    private async Task PickFolderAsync()
    {
        var folder = await _fileDialogs.PickFolderAsync().ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(folder))
        {
            return;
        }

        await ApplyFolderAndAnalyzeAsync(folder).ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(CanClear))]
    private void ClearAll()
    {
        _cts?.Cancel();
        _lastFolderPath = null;
        _selectedFiles.Clear();
        ResetOutputsAfterSelectionChange();
        StatusText =
            "Список файлов и результаты очищены. Добавьте PDF через «Выбрать PDF» или укажите папку.";
        AnalyzeCommand.NotifyCanExecuteChanged();
        ExportCsvCommand.NotifyCanExecuteChanged();
        ExportXlsxCommand.NotifyCanExecuteChanged();
    }

    private bool CanClear() => !IsBusy && (_selectedFiles.Count > 0 || Rows.Count > 0 || _lastReport is not null);

    [RelayCommand(CanExecute = nameof(CanAnalyze))]
    private async Task AnalyzeAsync()
    {
        if (_selectedFiles.Count == 0)
        {
            ResetOutputsBeforeRun();
            _lastReport = null;
            UpdateFormatStatLines();
            ExportCsvCommand.NotifyCanExecuteChanged();
            ExportXlsxCommand.NotifyCanExecuteChanged();
            return;
        }

        _cts = new CancellationTokenSource();
        IsBusy = true;
        ProgressValue = 0;
        ResetOutputsBeforeRun();
        ExportCsvCommand.NotifyCanExecuteChanged();
        ExportXlsxCommand.NotifyCanExecuteChanged();

        try
        {
            var progress = new Progress<BatchProgress>(p =>
            {
                if (p.TotalFiles > 0)
                {
                    ProgressValue = 100.0 * p.CompletedFiles / p.TotalFiles;
                }

                StatusText = $"Обработка ({p.CompletedFiles}/{p.TotalFiles}): {p.CurrentFile}";
            });

            var fileReports = new List<FileReport>();
            await foreach (var report in _batchPdfAnalyzer.AnalyzeFilesAsync(
                               _selectedFiles,
                               progress,
                               _cts.Token,
                               _options.Value.FormatToleranceMm)
                           .ConfigureAwait(true))
            {
                fileReports.Add(report);
                Rows.Add(
                    new FileReportRowViewModel
                    {
                        FilePath = report.FilePath,
                        PageCount = report.Pages.Count,
                        TotalLengthMeters = RoundMeters(report.TotalLengthMeters),
                        FormatsSummary = BuildFormatsSummary(report),
                        Error = report.Error,
                    });
            }

            _lastReport = PageAnalysisService.Combine(fileReports);
            TotalLengthMeters = RoundMeters(_lastReport.TotalLengthMeters);
            SummaryByFormat = BuildBatchSummary(_lastReport);
            UpdateFormatStatLines();
            StatusText =
                $"Готово: {_selectedFiles.Count} файл(ов), суммарно {TotalLengthMeters:F3} м по длинной стороне листов.";
            ProgressValue = 100;
            ExportCsvCommand.NotifyCanExecuteChanged();
            ExportXlsxCommand.NotifyCanExecuteChanged();
        }
        catch (OperationCanceledException)
        {
            ResetOutputsBeforeRun();
            _lastReport = null;
            UpdateFormatStatLines();
            StatusText = "Отменено.";
            ExportCsvCommand.NotifyCanExecuteChanged();
            ExportXlsxCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Analysis failed");
            ResetOutputsBeforeRun();
            _lastReport = null;
            UpdateFormatStatLines();
            StatusText = $"Ошибка: {ex.Message}";
            ExportCsvCommand.NotifyCanExecuteChanged();
            ExportXlsxCommand.NotifyCanExecuteChanged();
        }
        finally
        {
            IsBusy = false;
            _cts?.Dispose();
            _cts = null;
            AnalyzeCommand.NotifyCanExecuteChanged();
            ClearAllCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanAnalyze() => !IsBusy && _selectedFiles.Count > 0;

    [RelayCommand(CanExecute = nameof(CanExport))]
    private async Task ExportCsvAsync()
    {
        if (_lastReport is null)
        {
            return;
        }

        var path = await _fileDialogs.SaveFileAsync("CSV (*.csv)|*.csv", "report.csv").ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            await _reportWriter
                .WriteCsvAsync(
                    _lastReport,
                    path,
                    new ReportExportOptions(UseUtf8Bom: Utf8BomForCsv, CsvDelimiter: ';'),
                    CancellationToken.None)
                .ConfigureAwait(true);
            StatusText = $"Экспорт CSV: {path}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CSV export failed");
            StatusText = $"Ошибка экспорта CSV: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanExport))]
    private async Task ExportXlsxAsync()
    {
        if (_lastReport is null)
        {
            return;
        }

        var path = await _fileDialogs.SaveFileAsync("Excel (*.xlsx)|*.xlsx", "report.xlsx").ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            await _reportWriter
                .WriteXlsxAsync(_lastReport, path, CancellationToken.None)
                .ConfigureAwait(true);
            StatusText = $"Экспорт XLSX: {path}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "XLSX export failed");
            StatusText = $"Ошибка экспорта XLSX: {ex.Message}";
        }
    }

    private bool CanExport() => _lastReport is not null && !IsBusy;

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel()
    {
        _cts?.Cancel();
    }

    private bool CanCancel() => IsBusy && _cts is not null;

    partial void OnIsBusyChanged(bool value)
    {
        AnalyzeCommand.NotifyCanExecuteChanged();
        ExportCsvCommand.NotifyCanExecuteChanged();
        ExportXlsxCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
        ClearAllCommand.NotifyCanExecuteChanged();
    }

    private static string BuildFormatsSummary(FileReport report)
    {
        if (report.Error is not null)
        {
            return string.Empty;
        }

        return string.Join(
            ", ",
            report.ByFormat.OrderBy(k => k.Key, StringComparer.Ordinal)
                .Select(kv => $"{kv.Key}: {RoundMeters(kv.Value.LengthMeters)} м"));
    }

    private static string BuildBatchSummary(BatchReport report)
    {
        return string.Join(
            Environment.NewLine,
            report.SummaryByFormat.OrderBy(k => k.Key, StringComparer.Ordinal)
                .Select(kv => $"{kv.Key}: {kv.Value.PageCount} л., {RoundMeters(kv.Value.LengthMeters)} м"));
    }

    private static double RoundMeters(double m) =>
        Math.Round(m, MeasurementDefaults.LengthMetersDecimalPlaces, MidpointRounding.AwayFromZero);

    private void ApplyEnabledFormatsAndRecalculate()
    {
        var enabled = new List<string>(7);
        if (FormatA4) enabled.Add("A4");
        if (FormatA3) enabled.Add("A3");
        if (FormatA2) enabled.Add("A2");
        if (FormatA1) enabled.Add("A1");
        if (FormatA1Plus) enabled.Add("A1+");
        if (FormatA0) enabled.Add("A0");
        if (FormatA0Plus) enabled.Add("A0+");

        _formatRegistry.SetEnabledFormats(enabled);
        if (_selectedFiles.Count > 0 && !IsBusy)
        {
            _ = AnalyzeAsync();
        }
    }

    partial void OnFormatA4Changed(bool value) => ApplyEnabledFormatsAndRecalculate();
    partial void OnFormatA3Changed(bool value) => ApplyEnabledFormatsAndRecalculate();
    partial void OnFormatA2Changed(bool value) => ApplyEnabledFormatsAndRecalculate();
    partial void OnFormatA1Changed(bool value) => ApplyEnabledFormatsAndRecalculate();
    partial void OnFormatA1PlusChanged(bool value) => ApplyEnabledFormatsAndRecalculate();
    partial void OnFormatA0Changed(bool value) => ApplyEnabledFormatsAndRecalculate();
    partial void OnFormatA0PlusChanged(bool value) => ApplyEnabledFormatsAndRecalculate();

    partial void OnRecursiveFoldersChanged(bool value)
    {
        if (string.IsNullOrWhiteSpace(_lastFolderPath))
        {
            return;
        }

        _ = ReapplyLastFolderAsync();
    }

    private async Task ReapplyLastFolderAsync()
    {
        try
        {
            var folder = _lastFolderPath!;
            var files = PdfFileDiscovery.EnumeratePdfFilesInFolder(folder, RecursiveFolders);
            ReplaceSelectedPdfFiles(files);

            if (_selectedFiles.Count == 0)
            {
                StatusText =
                    "В этой папке не найдено PDF при текущем режиме (включён или выключен обход подпапок). Укажите другую папку.";
                AnalyzeCommand.NotifyCanExecuteChanged();
                return;
            }

            AnalyzeCommand.NotifyCanExecuteChanged();
            if (_autoAnalyzeAfterFileSelection)
            {
                StatusText = $"Папка: {folder}. PDF: {_selectedFiles.Count}. Запуск расчёта…";
                await AnalyzeAsync().ConfigureAwait(true);
                return;
            }

            StatusText =
                $"Папка: {folder}. PDF: {_selectedFiles.Count}. Список обновлён — нажмите «Считать».";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Recursive folder toggle failed");
            StatusText = $"Не удалось обновить список PDF: {ex.Message}";
        }
    }

    /// <summary>Заполняет <see cref="_selectedFiles"/> и сбрасывает результаты анализа (до нового расчёта).</summary>
    private void ReplaceSelectedPdfFiles(IReadOnlyList<string> pdfPaths)
    {
        _selectedFiles.Clear();
        _selectedFiles.AddRange(pdfPaths);
        ResetOutputsAfterSelectionChange();
    }

    private async Task ApplyFolderAndAnalyzeAsync(string folder)
    {
        _lastFolderPath = folder;
        var files = PdfFileDiscovery.EnumeratePdfFilesInFolder(folder, RecursiveFolders);
        ReplaceSelectedPdfFiles(files);

        if (_selectedFiles.Count == 0)
        {
            StatusText =
                "В выбранной папке не найдено PDF. Проверьте «Рекурсивно искать PDF в папке» или выберите другую папку.";
            AnalyzeCommand.NotifyCanExecuteChanged();
            return;
        }

        AnalyzeCommand.NotifyCanExecuteChanged();
        if (_autoAnalyzeAfterFileSelection)
        {
            StatusText = $"Папка: {folder}. PDF: {_selectedFiles.Count}. Запуск расчёта…";
            await AnalyzeAsync().ConfigureAwait(true);
            return;
        }

        StatusText =
            $"Папка: {folder}. PDF: {_selectedFiles.Count}. Нажмите «Считать». Переключатель «Рекурсивно» подхватывает ту же папку.";
    }

    private void ResetOutputsAfterSelectionChange()
    {
        ResetOutputsBeforeRun();
        _lastReport = null;
        UpdateFormatStatLines();
        ExportCsvCommand.NotifyCanExecuteChanged();
        ExportXlsxCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Очищает таблицу и сводные поля перед новым расчётом (без затрагивания выбора файлов).</summary>
    private void ResetOutputsBeforeRun()
    {
        Rows.Clear();
        TotalLengthMeters = 0;
        SummaryByFormat = string.Empty;
        ProgressValue = 0;
        ResetFormatStatLines();
        ClearAllCommand.NotifyCanExecuteChanged();
    }

    private void ResetFormatStatLines()
    {
        FormatStatA4 = "—";
        FormatStatA3 = "—";
        FormatStatA2 = "—";
        FormatStatA1 = "—";
        FormatStatA1Plus = "—";
        FormatStatA0 = "—";
        FormatStatA0Plus = "—";
    }

    private void UpdateFormatStatLines()
    {
        FormatStatA4 = FormatStatForLabel("A4");
        FormatStatA3 = FormatStatForLabel("A3");
        FormatStatA2 = FormatStatForLabel("A2");
        FormatStatA1 = FormatStatForLabel("A1");
        FormatStatA1Plus = FormatStatForLabel("A1+");
        FormatStatA0 = FormatStatForLabel("A0");
        FormatStatA0Plus = FormatStatForLabel("A0+");
    }

    private string FormatStatForLabel(string label)
    {
        if (_lastReport?.SummaryByFormat.TryGetValue(label, out var agg) == true && agg.PageCount > 0)
        {
            return $"{agg.PageCount} л., {RoundMeters(agg.LengthMeters)} м";
        }

        return "—";
    }
}
