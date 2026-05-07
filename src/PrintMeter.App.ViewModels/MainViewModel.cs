using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrintMeter.Core;
using PrintMeter.Core.Models;

namespace PrintMeter.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private static readonly StringComparer PathComparer = StringComparer.OrdinalIgnoreCase;

    private readonly IFormatRegistry _formatRegistry;
    private readonly BatchPdfAnalyzer _batchPdfAnalyzer;
    private readonly IBatchReportWriter _reportWriter;
    private readonly IFileDialogService _fileDialogs;
    private readonly IOptions<PrintMeterOptions> _options;
    private readonly ILogger<MainViewModel> _logger;
    private readonly bool _autoAnalyzeAfterFileSelection;
    private CancellationTokenSource? _cts;
    private readonly List<string> _selectedFiles = [];
    private readonly HashSet<string> _selectedSet = new(PathComparer);
    /// <summary>Нормализованные пути PDF, попавшие в выбор из последнего выбора папки (для замены при смене папки / рекурсии).</summary>
    private readonly HashSet<string> _canonicalFromLastFolder = new(PathComparer);
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
        "Добавляйте PDF по одному или папкой — строки накапливаются. «Считать заново» пересчитывает весь список. «Очистить» сбрасывает всё.";

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

    /// <summary>Условные листы по прайсу: по каждому ISO-формату Σ мм длинной стороны ÷ номинал (зашит в <see cref="PricelistFormatEquivalence.IsoNominalLongEdgeMm"/>).</summary>
    [ObservableProperty]
    private string _billingPricelistFormatsSummary = "—";

    private BatchReport? _lastReport;

    [RelayCommand]
    private async Task PickFilesAsync()
    {
        var picked = await _fileDialogs.PickPdfFilesAsync().ConfigureAwait(true);
        if (picked is null || picked.Count == 0)
        {
            return;
        }

        var newlyAdded = new List<string>();
        foreach (var raw in picked)
        {
            if (TryAddUniquePdf(raw, out var canon))
            {
                newlyAdded.Add(canon);
            }
        }

        AnalyzeCommand.NotifyCanExecuteChanged();
        ClearAllCommand.NotifyCanExecuteChanged();

        if (newlyAdded.Count == 0)
        {
            StatusText =
                $"Все выбранные файлы уже есть в списке ({_selectedFiles.Count} PDF). Добавьте другие или нажмите «Считать заново».";
            return;
        }

        if (_autoAnalyzeAfterFileSelection)
        {
            StatusText =
                $"Добавлено файлов: {newlyAdded.Count}. Всего в списке: {_selectedFiles.Count}. Запуск расчёта…";
            await AnalyzeAppendAsync(newlyAdded).ConfigureAwait(true);
            return;
        }

        StatusText =
            $"Добавлено файлов: {newlyAdded.Count}. Всего в списке: {_selectedFiles.Count}. Нажмите «Считать» для новых файлов или «Считать заново» для всего списка.";
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
        _canonicalFromLastFolder.Clear();
        _selectedFiles.Clear();
        _selectedSet.Clear();
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
        await AnalyzeFullAsync().ConfigureAwait(true);
    }

    private async Task AnalyzeFullAsync()
    {
        if (_selectedFiles.Count == 0)
        {
            _lastReport = null;
            ResetOutputsBeforeRun();
            UpdateFormatStatLines();
            ExportCsvCommand.NotifyCanExecuteChanged();
            ExportXlsxCommand.NotifyCanExecuteChanged();
            return;
        }

        _cts = new CancellationTokenSource();
        IsBusy = true;
        ProgressValue = 0;
        _lastReport = null;
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

    /// <summary>Обрабатывает только переданные пути и дополняет таблицу и сводку (без очистки существующих строк).</summary>
    private async Task AnalyzeAppendAsync(IReadOnlyList<string> canonicalPathsToAnalyze)
    {
        var todo = canonicalPathsToAnalyze
            .Where(c => !HasRowForNormalizedPath(c))
            .Distinct(PathComparer)
            .ToList();

        if (todo.Count == 0)
        {
            StatusText = $"Эти файлы уже обработаны. В таблице {Rows.Count} строк(и). Всего в списке: {_selectedFiles.Count}.";
            return;
        }

        _cts = new CancellationTokenSource();
        IsBusy = true;
        ProgressValue = 0;
        ExportCsvCommand.NotifyCanExecuteChanged();
        ExportXlsxCommand.NotifyCanExecuteChanged();

        var priorReports = (_lastReport?.Files ?? []).ToArray();
        var rowCountBefore = Rows.Count;

        try
        {
            var collected = new List<FileReport>();
            var progress = new Progress<BatchProgress>(p =>
            {
                if (p.TotalFiles > 0)
                {
                    ProgressValue = 100.0 * p.CompletedFiles / p.TotalFiles;
                }

                StatusText = $"Обработка ({p.CompletedFiles}/{p.TotalFiles}): {p.CurrentFile}";
            });

            await foreach (var report in _batchPdfAnalyzer.AnalyzeFilesAsync(
                               todo,
                               progress,
                               _cts.Token,
                               _options.Value.FormatToleranceMm)
                           .ConfigureAwait(true))
            {
                collected.Add(report);
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

            var mergedFileReports = new List<FileReport>(priorReports.Length + collected.Count);
            mergedFileReports.AddRange(priorReports);
            mergedFileReports.AddRange(collected);
            _lastReport = PageAnalysisService.Combine(mergedFileReports);
            TotalLengthMeters = RoundMeters(_lastReport.TotalLengthMeters);
            SummaryByFormat = BuildBatchSummary(_lastReport);
            UpdateFormatStatLines();
            StatusText =
                $"Готово: добавлено {collected.Count} файл(ов). В таблице {Rows.Count} строк(и), суммарно {TotalLengthMeters:F3} м.";
            ProgressValue = 100;
            ExportCsvCommand.NotifyCanExecuteChanged();
            ExportXlsxCommand.NotifyCanExecuteChanged();
        }
        catch (OperationCanceledException)
        {
            RollbackAppend(rowCountBefore, priorReports);
            StatusText = "Отменено.";
            ExportCsvCommand.NotifyCanExecuteChanged();
            ExportXlsxCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Incremental analysis failed");
            RollbackAppend(rowCountBefore, priorReports);
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

    private void RollbackAppend(int rowCountBefore, IReadOnlyList<FileReport> priorReports)
    {
        while (Rows.Count > rowCountBefore)
        {
            Rows.RemoveAt(Rows.Count - 1);
        }

        _lastReport = priorReports.Count > 0 ? PageAnalysisService.Combine(priorReports) : null;
        if (_lastReport is null)
        {
            TotalLengthMeters = 0;
            SummaryByFormat = string.Empty;
        }
        else
        {
            TotalLengthMeters = RoundMeters(_lastReport.TotalLengthMeters);
            SummaryByFormat = BuildBatchSummary(_lastReport);
        }

        UpdateFormatStatLines();

        ProgressValue = 0;
        ClearAllCommand.NotifyCanExecuteChanged();
    }

    private ReportExportOptions BuildExportOptions()
    {
        var pricelist =
            _lastReport is null
                ? null
                : PricelistFormatEquivalence.BuildExportAttachment(
                    _lastReport.SummaryByFormat,
                    null,
                    PricelistFormatEquivalence.DefaultRounding);

        return new ReportExportOptions(Utf8BomForCsv, ';', pricelist);
    }

    private void RefreshBillingPricelistFormatsSummary()
    {
        if (_lastReport is null)
        {
            BillingPricelistFormatsSummary = "—";
            return;
        }

        var mode = PricelistFormatEquivalence.DefaultRounding;
        var rows = PricelistFormatEquivalence.ComputeRows(
            _lastReport.SummaryByFormat,
            overridesMm: null,
            mode);

        if (rows.Count == 0)
        {
            BillingPricelistFormatsSummary =
                "Прайс по форматам: нет ISO-меток в сводке (или только Custom / нулевой метраж). Номиналы A4…A0+ заданы в коде.";
            return;
        }

        var inv = CultureInfo.InvariantCulture;
        var roundRu = mode switch
        {
            PricelistFormatEquivalence.RoundingMode.Ceiling => "округление вверх до целого условного листа",
            PricelistFormatEquivalence.RoundingMode.NearestAwayFromZero => "к ближайшему целому",
            _ => mode.ToString(),
        };

        var lines = rows.Select(
            r =>
                $"{r.FormatLabel}: {r.BillingSheets.ToString(inv)} условн. ({r.CombinedLongMm.ToString("0.#", inv)} мм ÷ {r.DivisorMm.ToString("0.#", inv)} мм → {r.RawSheets.ToString("0.###", inv)}, {roundRu})");

        BillingPricelistFormatsSummary =
            "Условные листы под прайс (знаменатель = номинальная длинная сторона формата из ISO, см. код):" +
            Environment.NewLine + string.Join(Environment.NewLine, lines);
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
                    BuildExportOptions(),
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
                .WriteXlsxAsync(_lastReport, path, BuildExportOptions(), CancellationToken.None)
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
            _ = AnalyzeFullAsync();
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
            ApplyFolderPdfSelection(files);

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
                StatusText = $"Папка: {folder}. PDF в выборе: {_selectedFiles.Count}. Запуск расчёта…";
                await AnalyzeNewAndOrphanRowsAsync().ConfigureAwait(true);
                return;
            }

            StatusText =
                $"Папка: {folder}. PDF в выборе: {_selectedFiles.Count}. Нажмите «Считать» для новых файлов или «Считать заново».";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Recursive folder toggle failed");
            StatusText = $"Не удалось обновить список PDF: {ex.Message}";
        }
    }

    private async Task ApplyFolderAndAnalyzeAsync(string folder)
    {
        _lastFolderPath = folder;
        var files = PdfFileDiscovery.EnumeratePdfFilesInFolder(folder, RecursiveFolders);
        ApplyFolderPdfSelection(files);

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
            StatusText = $"Папка: {folder}. PDF в выборе: {_selectedFiles.Count}. Запуск расчёта…";
            await AnalyzeNewAndOrphanRowsAsync().ConfigureAwait(true);
            return;
        }

        StatusText =
            $"Папка: {folder}. PDF в выборе: {_selectedFiles.Count}. Нажмите «Считать» или «Считать заново». Переключатель «Рекурсивно» обновляет состав из той же папки.";
    }

    /// <summary>Убирает из выбора PDF из прошлого перечисления папки, добавляет новый список и подчищает устаревшие строки.</summary>
    private void ApplyFolderPdfSelection(IReadOnlyList<string> rawPdfPathsFromFolder)
    {
        var newCanonSet = new HashSet<string>(PathComparer);
        foreach (var p in rawPdfPathsFromFolder)
        {
            newCanonSet.Add(NormalizePath(p));
        }

        RemoveSelectionPaths(_canonicalFromLastFolder);
        _canonicalFromLastFolder.Clear();
        foreach (var c in newCanonSet)
        {
            _canonicalFromLastFolder.Add(c);
        }

        foreach (var raw in rawPdfPathsFromFolder)
        {
            TryAddUniquePdf(raw, out _);
        }

        PruneRowsAndReportToMatchSelection();
    }

    private void RemoveSelectionPaths(IEnumerable<string> normalizedPaths)
    {
        foreach (var c in normalizedPaths)
        {
            if (_selectedSet.Remove(c))
            {
                _selectedFiles.RemoveAll(p => PathComparer.Equals(p, c));
            }
        }
    }

    private void PruneRowsAndReportToMatchSelection()
    {
        for (var i = Rows.Count - 1; i >= 0; i--)
        {
            var rowPath = NormalizePath(Rows[i].FilePath);
            if (!_selectedSet.Contains(rowPath))
            {
                Rows.RemoveAt(i);
            }
        }

        if (_lastReport is null)
        {
            UpdateFormatStatLines();
            ClearAllCommand.NotifyCanExecuteChanged();
            return;
        }

        var kept = _lastReport.Files
            .Where(f => _selectedSet.Contains(NormalizePath(f.FilePath)))
            .ToList();
        _lastReport = kept.Count > 0 ? PageAnalysisService.Combine(kept) : null;
        if (_lastReport is null)
        {
            TotalLengthMeters = 0;
            SummaryByFormat = string.Empty;
        }
        else
        {
            TotalLengthMeters = RoundMeters(_lastReport.TotalLengthMeters);
            SummaryByFormat = BuildBatchSummary(_lastReport);
        }

        UpdateFormatStatLines();

        ExportCsvCommand.NotifyCanExecuteChanged();
        ExportXlsxCommand.NotifyCanExecuteChanged();
        ClearAllCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Анализирует файлы из списка, по которым ещё нет строки (после смены папки / рекурсии).</summary>
    private async Task AnalyzeNewAndOrphanRowsAsync()
    {
        var missing = _selectedFiles.Where(f => !HasRowForNormalizedPath(f)).ToList();
        if (missing.Count == 0)
        {
            StatusText =
                $"Состав папки обновлён. В таблице {Rows.Count} файл(ов), суммарно {TotalLengthMeters:F3} м (по длинной стороне).";
            return;
        }

        await AnalyzeAppendAsync(missing).ConfigureAwait(true);
    }

    private bool HasRowForNormalizedPath(string canonicalPath)
    {
        foreach (var row in Rows)
        {
            if (PathComparer.Equals(NormalizePath(row.FilePath), canonicalPath))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryAddUniquePdf(string rawPath, out string normalized)
    {
        normalized = NormalizePath(rawPath);
        if (!_selectedSet.Add(normalized))
        {
            return false;
        }

        _selectedFiles.Add(normalized);
        return true;
    }

    private static string NormalizePath(string path)
    {
        try
        {
            return Path.GetFullPath(path);
        }
        catch
        {
            return path.Trim();
        }
    }

    private void ResetOutputsAfterSelectionChange()
    {
        _lastReport = null;
        ResetOutputsBeforeRun();
        UpdateFormatStatLines();
        ExportCsvCommand.NotifyCanExecuteChanged();
        ExportXlsxCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Очищает таблицу и сводные поля перед полным пересчётом (без затрагивания выбора файлов).</summary>
    private void ResetOutputsBeforeRun()
    {
        Rows.Clear();
        TotalLengthMeters = 0;
        SummaryByFormat = string.Empty;
        ProgressValue = 0;
        ResetFormatStatLines();
        RefreshBillingPricelistFormatsSummary();
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
        RefreshBillingPricelistFormatsSummary();
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
