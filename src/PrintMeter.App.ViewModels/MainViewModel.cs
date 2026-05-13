using System.Collections.ObjectModel;
using System.Globalization;
using System.ComponentModel;
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
    private readonly IFileDialogService _fileDialogs;
    private readonly IUiStrings _s;
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
        IFileDialogService fileDialogs,
        IUiStrings uiStrings,
        IOptions<PrintMeterOptions> options,
        ILogger<MainViewModel> logger,
        bool autoAnalyzeAfterFileSelection = true)
    {
        _formatRegistry = formatRegistry;
        _batchPdfAnalyzer = batchPdfAnalyzer;
        _fileDialogs = fileDialogs;
        _s = uiStrings;
        _options = options;
        _logger = logger;
        _autoAnalyzeAfterFileSelection = autoAnalyzeAfterFileSelection;

        StatusText = _s.Format(UiKeys.StatusIntro);
        InitializeFormatRows(_formatRegistry.EnabledFormats);
    }

    public ObservableCollection<FileReportRowViewModel> Rows { get; } = new();
    public ObservableCollection<FormatToggleRowViewModel> FormatRows { get; } = new();

    public string BillingKpiTooltip => _s.Format(UiKeys.BillingKpiTooltip);

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private double _progressValue;

    public string ProgressPercentText =>
        _s.Format(UiKeys.ProgressPercent, Math.Round(ProgressValue, 0));

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private double _totalLengthMeters;

    [ObservableProperty]
    private string _summaryByFormat = string.Empty;

    [ObservableProperty]
    private bool _recursiveFolders = true;

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
            StatusText = _s.Format(UiKeys.AllPdfsAlreadyInList, _selectedFiles.Count);
            return;
        }

        if (_autoAnalyzeAfterFileSelection)
        {
            StatusText = _s.Format(UiKeys.AddedRunning, newlyAdded.Count, _selectedFiles.Count);
            await AnalyzeAppendAsync(newlyAdded).ConfigureAwait(true);
            return;
        }

        StatusText = _s.Format(UiKeys.AddedNeedAnalyze, newlyAdded.Count, _selectedFiles.Count);
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
        StatusText = _s.Format(UiKeys.ClearDone);
        AnalyzeCommand.NotifyCanExecuteChanged();
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
            return;
        }

        _cts = new CancellationTokenSource();
        IsBusy = true;
        ProgressValue = 0;
        _lastReport = null;
        ResetOutputsBeforeRun();

        try
        {
            var progress = new Progress<BatchProgress>(p =>
            {
                if (p.TotalFiles > 0)
                {
                    ProgressValue = 100.0 * p.CompletedFiles / p.TotalFiles;
                }

                StatusText = _s.Format(UiKeys.Processing, p.CompletedFiles, p.TotalFiles, p.CurrentFile ?? string.Empty);
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
                        IssueText = report.Error,
                    });
            }

            _lastReport = PageAnalysisService.Combine(fileReports);
            TotalLengthMeters = RoundMeters(_lastReport.TotalLengthMeters);
            SummaryByFormat = BuildBatchSummary(_lastReport);
            UpdateFormatStatLines();
            StatusText = _s.Format(UiKeys.DoneFull, _selectedFiles.Count, TotalLengthMeters);
            ProgressValue = 100;
        }
        catch (OperationCanceledException)
        {
            ResetOutputsBeforeRun();
            _lastReport = null;
            UpdateFormatStatLines();
            StatusText = _s.Format(UiKeys.Cancelled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Analysis failed");
            ResetOutputsBeforeRun();
            _lastReport = null;
            UpdateFormatStatLines();
            StatusText = _s.Format(UiKeys.Error, ex.Message);
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
            StatusText = _s.Format(UiKeys.AlreadyProcessed, Rows.Count, _selectedFiles.Count);
            return;
        }

        _cts = new CancellationTokenSource();
        IsBusy = true;
        ProgressValue = 0;

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

                StatusText = _s.Format(UiKeys.Processing, p.CompletedFiles, p.TotalFiles, p.CurrentFile ?? string.Empty);
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
                        IssueText = report.Error,
                    });
            }

            var mergedFileReports = new List<FileReport>(priorReports.Length + collected.Count);
            mergedFileReports.AddRange(priorReports);
            mergedFileReports.AddRange(collected);
            _lastReport = PageAnalysisService.Combine(mergedFileReports);
            TotalLengthMeters = RoundMeters(_lastReport.TotalLengthMeters);
            SummaryByFormat = BuildBatchSummary(_lastReport);
            UpdateFormatStatLines();
            StatusText = _s.Format(UiKeys.DoneAppend, collected.Count, Rows.Count, TotalLengthMeters);
            ProgressValue = 100;
        }
        catch (OperationCanceledException)
        {
            RollbackAppend(rowCountBefore, priorReports);
            StatusText = _s.Format(UiKeys.Cancelled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Incremental analysis failed");
            RollbackAppend(rowCountBefore, priorReports);
            StatusText = _s.Format(UiKeys.Error, ex.Message);
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
            BillingPricelistFormatsSummary = _s.Format(UiKeys.BillingNoIso);
            return;
        }

        var inv = CultureInfo.InvariantCulture;
        var roundText = mode switch
        {
            PricelistFormatEquivalence.RoundingMode.Ceiling => _s.Format(UiKeys.BillingRoundingCeiling),
            PricelistFormatEquivalence.RoundingMode.NearestAwayFromZero => _s.Format(UiKeys.BillingRoundingNearest),
            _ => mode.ToString(),
        };

        var labelW = rows.Max(r => r.FormatLabel.Length);
        labelW = Math.Max(labelW, 4);

        var lines = rows.Select(
            r =>
                _s.Format(
                    UiKeys.BillingRow,
                    r.FormatLabel.PadRight(labelW),
                    r.BillingSheets.ToString(inv).PadLeft(4),
                    r.CombinedLongMm.ToString("0.#", inv).PadLeft(7),
                    r.DivisorMm.ToString("0.#", inv),
                    r.RawSheets.ToString("0.###", inv)));

        BillingPricelistFormatsSummary =
            _s.Format(UiKeys.BillingIntro)
            + Environment.NewLine
            + _s.Format(UiKeys.BillingRoundingLine, roundText)
            + Environment.NewLine
            + string.Join(Environment.NewLine, lines);
    }

    private bool CanAnalyze() => !IsBusy && _selectedFiles.Count > 0;

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel()
    {
        _cts?.Cancel();
    }

    private bool CanCancel() => IsBusy && _cts is not null;

    partial void OnIsBusyChanged(bool value)
    {
        AnalyzeCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
        ClearAllCommand.NotifyCanExecuteChanged();
    }

    partial void OnProgressValueChanged(double value) =>
        OnPropertyChanged(nameof(ProgressPercentText));

    private string BuildFormatsSummary(FileReport report)
    {
        if (report.Error is not null)
        {
            return string.Empty;
        }

        return string.Join(
            " · ",
            report.ByFormat.OrderBy(k => k.Key, StringComparer.Ordinal)
                .Select(
                    kv => _s.Format(
                        UiKeys.FormatSummaryEntry,
                        kv.Key,
                        kv.Value.PageCount,
                        RoundMeters(kv.Value.LengthMeters))));
    }

    private string BuildBatchSummary(BatchReport report)
    {
        var inv = CultureInfo.InvariantCulture;
        var ordered = report.SummaryByFormat.OrderBy(k => k.Key, StringComparer.Ordinal).ToList();
        if (ordered.Count == 0)
        {
            return string.Empty;
        }

        var labelW = Math.Max(ordered.Max(kv => kv.Key.Length), 6);
        var formatCol = _s.Format(UiKeys.BatchSummaryFormatColumn);
        var head = _s.Format(UiKeys.BatchSummaryHeader, formatCol.PadRight(labelW));
        var body = ordered.Select(
            kv =>
                _s.Format(
                    UiKeys.BatchSummaryRow,
                    kv.Key.PadRight(labelW),
                    kv.Value.PageCount,
                    RoundMeters(kv.Value.LengthMeters)));
        return string.Join(Environment.NewLine, new[] { head }.Concat(body));
    }

    private static double RoundMeters(double m) =>
        Math.Round(m, MeasurementDefaults.LengthMetersDecimalPlaces, MidpointRounding.AwayFromZero);

    private void ApplyEnabledFormatsAndRecalculate()
    {
        var enabled = FormatRows
            .Where(row => row.IsEnabled)
            .Select(row => row.Key)
            .ToList();

        _formatRegistry.SetEnabledFormats(enabled);
        if (_selectedFiles.Count > 0 && !IsBusy)
        {
            _ = AnalyzeFullAsync();
        }
    }

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
                StatusText = _s.Format(UiKeys.FolderNoPdfsRecursive);
                AnalyzeCommand.NotifyCanExecuteChanged();
                return;
            }

            AnalyzeCommand.NotifyCanExecuteChanged();
            if (_autoAnalyzeAfterFileSelection)
            {
                StatusText = _s.Format(UiKeys.FolderRunning, folder, _selectedFiles.Count);
                await AnalyzeNewAndOrphanRowsAsync().ConfigureAwait(true);
                return;
            }

            StatusText = _s.Format(UiKeys.FolderNeedAnalyze, folder, _selectedFiles.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Recursive folder toggle failed");
            StatusText = _s.Format(UiKeys.ListRefreshFailed, ex.Message);
        }
    }

    private async Task ApplyFolderAndAnalyzeAsync(string folder)
    {
        _lastFolderPath = folder;
        var files = PdfFileDiscovery.EnumeratePdfFilesInFolder(folder, RecursiveFolders);
        ApplyFolderPdfSelection(files);

        if (_selectedFiles.Count == 0)
        {
            StatusText = _s.Format(UiKeys.FolderNoPdfs);
            AnalyzeCommand.NotifyCanExecuteChanged();
            return;
        }

        AnalyzeCommand.NotifyCanExecuteChanged();
        if (_autoAnalyzeAfterFileSelection)
        {
            StatusText = _s.Format(UiKeys.FolderRunning, folder, _selectedFiles.Count);
            await AnalyzeNewAndOrphanRowsAsync().ConfigureAwait(true);
            return;
        }

        StatusText = _s.Format(UiKeys.FolderAnalyzeHint, folder, _selectedFiles.Count);
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

        ClearAllCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Анализирует файлы из списка, по которым ещё нет строки (после смены папки / рекурсии).</summary>
    private async Task AnalyzeNewAndOrphanRowsAsync()
    {
        var missing = _selectedFiles.Where(f => !HasRowForNormalizedPath(f)).ToList();
        if (missing.Count == 0)
        {
            StatusText = _s.Format(UiKeys.FolderUpdated, Rows.Count, TotalLengthMeters);
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
        foreach (var row in FormatRows)
        {
            row.StatText = "—";
        }
    }

    private void UpdateFormatStatLines()
    {
        foreach (var row in FormatRows)
        {
            row.StatText = FormatStatForLabel(row.Key);
        }

        RefreshBillingPricelistFormatsSummary();
    }

    private void InitializeFormatRows(IReadOnlyCollection<string> enabledFormats)
    {
        AddFormatRow("A4", "A4", enabledFormats.Contains("A4"), 13);
        AddFormatRow("A3", "A3", enabledFormats.Contains("A3"), 15);
        AddFormatRow("A2", "A2", enabledFormats.Contains("A2"), 17);
        AddFormatRow("A1", "A1", enabledFormats.Contains("A1"), 19, _s.Format(UiKeys.FormatTooltipA1));
        AddFormatRow("A0", "A0", enabledFormats.Contains("A0"), 21, _s.Format(UiKeys.FormatTooltipA0));
    }

    private void AddFormatRow(string key, string label, bool isEnabled, double iconSize, string? tooltip = null)
    {
        var row = new FormatToggleRowViewModel(key, label, iconSize, tooltip)
        {
            IsEnabled = isEnabled
        };
        row.PropertyChanged += OnFormatRowPropertyChanged;
        FormatRows.Add(row);
    }

    private void OnFormatRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FormatToggleRowViewModel.IsEnabled))
        {
            ApplyEnabledFormatsAndRecalculate();
        }
    }

    private string FormatStatForLabel(string label)
    {
        if (_lastReport?.SummaryByFormat.TryGetValue(label, out var agg) == true && agg.PageCount > 0)
        {
            return _s.Format(UiKeys.FormatStatLine, agg.PageCount, RoundMeters(agg.LengthMeters));
        }

        return "—";
    }
}
