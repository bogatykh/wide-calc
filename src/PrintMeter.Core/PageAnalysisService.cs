using PrintMeter.Core.Models;

namespace PrintMeter.Core;

public sealed class PageAnalysisService(IFormatRegistry formatRegistry)
{
    public PageAnalysis AnalyzePage(PageDimensions page, double toleranceMm)
    {
        var widthMm = PdfUnits.PointsToMillimeters(page.WidthPt);
        var heightMm = PdfUnits.PointsToMillimeters(page.HeightPt);
        var longMm = Math.Max(widthMm, heightMm);
        var shortMm = Math.Min(widthMm, heightMm);
        var label = formatRegistry.ResolveLabel(longMm, shortMm, toleranceMm);
        var lengthM = MeasurementDefaults.PageLengthMeters(widthMm, heightMm);
        return new PageAnalysis(
            page.PageNumber,
            widthMm,
            heightMm,
            longMm,
            shortMm,
            label,
            lengthM);
    }

    public FileReport BuildFileReport(string filePath, IReadOnlyList<PageDimensions> pages, double toleranceMm)
    {
        var analyses = new List<PageAnalysis>(pages.Count);
        var byFormat = new Dictionary<string, FormatAggregate>(StringComparer.Ordinal);
        double total = 0;

        foreach (var p in pages)
        {
            var a = AnalyzePage(p, toleranceMm);
            analyses.Add(a);
            total += a.PageLengthMeters;
            if (!byFormat.TryGetValue(a.FormatLabel, out var agg))
            {
                agg = new FormatAggregate(0, 0);
            }

            byFormat[a.FormatLabel] = new FormatAggregate(
                agg.PageCount + 1,
                agg.LengthMeters + a.PageLengthMeters);
        }

        return new FileReport
        {
            FilePath = filePath,
            Pages = analyses,
            ByFormat = byFormat,
            TotalLengthMeters = total,
        };
    }

    public static BatchReport Combine(IReadOnlyList<FileReport> files)
    {
        var summary = new Dictionary<string, FormatAggregate>(StringComparer.Ordinal);
        double grandTotal = 0;

        foreach (var file in files)
        {
            if (file.Error is not null)
            {
                continue;
            }

            grandTotal += file.TotalLengthMeters;
            foreach (var (key, agg) in file.ByFormat)
            {
                if (!summary.TryGetValue(key, out var s))
                {
                    s = new FormatAggregate(0, 0);
                }

                summary[key] = new FormatAggregate(s.PageCount + agg.PageCount, s.LengthMeters + agg.LengthMeters);
            }
        }

        return new BatchReport
        {
            Files = files,
            SummaryByFormat = summary,
            TotalLengthMeters = grandTotal,
        };
    }
}
