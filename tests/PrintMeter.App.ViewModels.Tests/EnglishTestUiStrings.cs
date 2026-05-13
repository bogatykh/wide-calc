using System.Globalization;
using PrintMeter.App.ViewModels;

namespace PrintMeter.App.ViewModels.Tests;

/// <summary>Английские шаблоны как в Strings/en-US/Resources.resw — без WinRT/PRI в unit-тестах.</summary>
internal sealed class EnglishTestUiStrings : IUiStrings
{
    private static readonly Dictionary<string, string> Templates = Build();

    public string Format(string resourceKey, params object[] args)
    {
        if (!Templates.TryGetValue(resourceKey, out var template))
        {
            return resourceKey;
        }

        return args.Length == 0
            ? template
            : string.Format(CultureInfo.InvariantCulture, template, args);
    }

    private static Dictionary<string, string> Build()
    {
        var d = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [UiKeys.StatusIntro] =
                "Add PDFs individually or by folder — rows accumulate. “Recalculate” recomputes the whole list. “Clear” resets everything.",
            [UiKeys.ProgressPercent] = "Progress: {0:F0}%",
            [UiKeys.AllPdfsAlreadyInList] =
                "All selected files are already in the list ({0} PDF). Add others or click Recalculate.",
            [UiKeys.AddedRunning] = "Added files: {0}. Total in list: {1}. Starting analysis…",
            [UiKeys.AddedNeedAnalyze] =
                "Added files: {0}. Total in list: {1}. Click Analyze for new files or Recalculate for the full list.",
            [UiKeys.ClearDone] = "File list and results cleared. Add PDFs via Choose PDF or pick a folder.",
            [UiKeys.Processing] = "Processing ({0}/{1}): {2}",
            [UiKeys.DoneFull] = "Done: {0} file(s), total {1:F3} m long-edge.",
            [UiKeys.Cancelled] = "Cancelled.",
            [UiKeys.Error] = "Error: {0}",
            [UiKeys.AlreadyProcessed] =
                "These files are already processed. Table has {0} row(s). Total in list: {1}.",
            [UiKeys.DoneAppend] = "Done: added {0} file(s). Table has {1} row(s), total {2:F3} m.",
            [UiKeys.BillingNoIso] =
                "Price by format: no ISO labels in the summary (or only Custom / zero length). A4…A0 nominals are defined in code.",
            [UiKeys.BillingRoundingCeiling] = "round up to whole equivalent sheet",
            [UiKeys.BillingRoundingNearest] = "to nearest integer",
            [UiKeys.BillingRow] = "  {0}  {1} eq.   Σ {2} mm ÷ {3} mm  →  {4} raw",
            [UiKeys.BillingIntro] =
                "Equivalent sheets for pricing: per ISO format, summed long-edge mm ÷ nominal from the in-code table.",
            [UiKeys.BillingRoundingLine] = "Rounding: {0}.",
            [UiKeys.FormatSummaryEntry] = "{0}: {1} sh · {2} m",
            [UiKeys.BatchSummaryFormatColumn] = "Format",
            [UiKeys.BatchSummaryHeader] = "{0}    Sheets    Meters (long edge)",
            [UiKeys.BatchSummaryRow] = "{0}  {1,5} sh  {2,8:F3} m",
            [UiKeys.FolderNoPdfsRecursive] =
                "No PDFs in this folder with the current mode (subfolders on or off). Pick another folder.",
            [UiKeys.FolderRunning] = "Folder: {0}. PDFs in selection: {1}. Starting analysis…",
            [UiKeys.FolderNeedAnalyze] =
                "Folder: {0}. PDFs in selection: {1}. Click Analyze for new files or Recalculate.",
            [UiKeys.ListRefreshFailed] = "Could not refresh the PDF list: {0}",
            [UiKeys.FolderNoPdfs] =
                "No PDFs in the selected folder. Check “Search PDFs recursively” or pick another folder.",
            [UiKeys.FolderAnalyzeHint] =
                "Folder: {0}. PDFs in selection: {1}. Click Analyze or Recalculate. The Recursive toggle refreshes the set from the same folder.",
            [UiKeys.FolderUpdated] =
                "Folder contents updated. Table has {0} file(s), total {1:F3} m (long edge).",
            [UiKeys.FormatStatLine] = "{0} sh, {1} m",
            [UiKeys.FormatTooltipA1] = "Short edge up to 610 mm (former A1+ counts as A1).",
            [UiKeys.FormatTooltipA0] = "Short edge up to 914 mm (former A0+ counts as A0).",
            [UiKeys.BillingKpiTooltip] =
                "Matches the format summary at the bottom: for each ISO label, long-edge mm are summed, then divided by the nominal from code.",
        };
        return d;
    }
}
