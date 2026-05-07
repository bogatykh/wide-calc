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
    private CancellationTokenSource? _cts;
    private readonly List<string> _selectedFiles = [];

    public MainViewModel(
        IFormatRegistry formatRegistry,
        BatchPdfAnalyzer batchPdfAnalyzer,
        IBatchReportWriter reportWriter,
        IFileDialogService fileDialogs,
        IOptions<PrintMeterOptions> options,
        ILogger<MainViewModel> logger)
    {
        _formatRegistry = formatRegistry;
        _batchPdfAnalyzer = batchPdfAnalyzer;
        _reportWriter = reportWriter;
        _fileDialogs = fileDialogs;
        _options = options;
        _logger = logger;

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
    private string _statusText = "Выберите PDF-файлы или папку.";

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

    private BatchReport? _lastReport;

    [RelayCommand]
    private async Task PickFilesAsync()
    {
        var picked = await _fileDialogs.PickPdfFilesAsync().ConfigureAwait(true);
        if (picked is null || picked.Count == 0)
        {
            return;
        }

        _selectedFiles.Clear();
        _selectedFiles.AddRange(picked);
        StatusText = $"Выбрано файлов: {_selectedFiles.Count}";
        AnalyzeCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private async Task PickFolderAsync()
    {
        var folder = await _fileDialogs.PickFolderAsync().ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(folder))
        {
            return;
        }

        _selectedFiles.Clear();
        var files = PdfFileDiscovery.EnumeratePdfFilesInFolder(folder, RecursiveFolders);
        _selectedFiles.AddRange(files);
        StatusText = $"Папка: {folder}. PDF: {_selectedFiles.Count}";
        AnalyzeCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanAnalyze))]
    private async Task AnalyzeAsync()
    {
        if (_selectedFiles.Count == 0)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        IsBusy = true;
        ProgressValue = 0;
        Rows.Clear();
        TotalLengthMeters = 0;
        SummaryByFormat = string.Empty;
        _lastReport = null;
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
            StatusText = "Готово.";
            ProgressValue = 100;
            ExportCsvCommand.NotifyCanExecuteChanged();
            ExportXlsxCommand.NotifyCanExecuteChanged();
        }
        catch (OperationCanceledException)
        {
            StatusText = "Отменено.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Analysis failed");
            StatusText = $"Ошибка: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            _cts?.Dispose();
            _cts = null;
            AnalyzeCommand.NotifyCanExecuteChanged();
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
                .Select(kv => $"{kv.Key}: {kv.Value.PageCount} стр., {RoundMeters(kv.Value.LengthMeters)} м"));
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
        // Re-pick folder semantics: user may toggle before picking folder again — no-op.
    }
}
