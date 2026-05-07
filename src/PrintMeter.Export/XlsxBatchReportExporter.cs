using System.Globalization;
using ClosedXML.Excel;
using PrintMeter.Core;
using PrintMeter.Core.Models;

namespace PrintMeter.Export;

public sealed class XlsxBatchReportExporter : IReportExporter
{
    public Task ExportAsync(
        BatchReport report,
        string destinationPath,
        ReportExportOptions options,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var culture = CultureInfo.InvariantCulture;

        using var workbook = new XLWorkbook();

        var summarySheet = workbook.AddWorksheet("Summary");
        summarySheet.Cell(1, 1).Value = "Format";
        summarySheet.Cell(1, 2).Value = "PageCount";
        summarySheet.Cell(1, 3).Value = "LengthMeters";
        var row = 2;
        foreach (var kv in report.SummaryByFormat.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            summarySheet.Cell(row, 1).Value = kv.Key;
            summarySheet.Cell(row, 2).Value = kv.Value.PageCount;
            summarySheet.Cell(row, 3).Value = Round(kv.Value.LengthMeters);
            summarySheet.Cell(row, 3).Style.NumberFormat.Format = "0.000";
            row++;
        }

        summarySheet.Cell(row + 1, 1).Value = "TotalLengthMeters";
        summarySheet.Cell(row + 1, 2).Value = Round(report.TotalLengthMeters);
        summarySheet.Cell(row + 1, 2).Style.NumberFormat.Format = "0.000";

        if (options.PricelistEquivalence is { } pe)
        {
            row += 2;
            summarySheet.Cell(row, 1).Value = "PricingFormatEquivalence";
            summarySheet.Cell(row, 2).Value = pe.RoundingModeKey;
            row++;
            summarySheet.Cell(row, 1).Value = "Format";
            summarySheet.Cell(row, 2).Value = "CombinedLongMm";
            summarySheet.Cell(row, 3).Value = "DivisorMm";
            summarySheet.Cell(row, 4).Value = "RawSheets";
            summarySheet.Cell(row, 5).Value = "BillingSheets";
            row++;
            foreach (var r in pe.PerFormatRows)
            {
                summarySheet.Cell(row, 1).Value = r.FormatLabel;
                summarySheet.Cell(row, 2).Value = r.CombinedLongMm;
                summarySheet.Cell(row, 2).Style.NumberFormat.Format = "0.0";
                summarySheet.Cell(row, 3).Value = r.DivisorMm;
                summarySheet.Cell(row, 4).Value = r.RawSheets;
                summarySheet.Cell(row, 5).Value = r.BillingSheets;
                row++;
            }
        }

        var detailSheet = workbook.AddWorksheet("Files");
        detailSheet.Cell(1, 1).Value = "FilePath";
        detailSheet.Cell(1, 2).Value = "Pages";
        detailSheet.Cell(1, 3).Value = "TotalLengthMeters";
        detailSheet.Cell(1, 4).Value = "Error";
        row = 2;
        foreach (var file in report.Files)
        {
            detailSheet.Cell(row, 1).Value = file.FilePath;
            detailSheet.Cell(row, 2).Value = file.Pages.Count;
            detailSheet.Cell(row, 3).Value = Round(file.TotalLengthMeters);
            detailSheet.Cell(row, 3).Style.NumberFormat.Format = "0.000";
            detailSheet.Cell(row, 4).Value = file.Error ?? string.Empty;
            row++;
        }

        var pagesSheet = workbook.AddWorksheet("Pages");
        pagesSheet.Cell(1, 1).Value = "FilePath";
        pagesSheet.Cell(1, 2).Value = "Page";
        pagesSheet.Cell(1, 3).Value = "WidthMm";
        pagesSheet.Cell(1, 4).Value = "HeightMm";
        pagesSheet.Cell(1, 5).Value = "Format";
        pagesSheet.Cell(1, 6).Value = "PageLengthMeters";
        row = 2;
        foreach (var file in report.Files)
        {
            if (file.Error is not null)
            {
                continue;
            }

            foreach (var page in file.Pages)
            {
                pagesSheet.Cell(row, 1).Value = file.FilePath;
                pagesSheet.Cell(row, 2).Value = page.PageNumber;
                pagesSheet.Cell(row, 3).Value = RoundMm(page.WidthMm);
                pagesSheet.Cell(row, 4).Value = RoundMm(page.HeightMm);
                pagesSheet.Cell(row, 5).Value = page.FormatLabel;
                pagesSheet.Cell(row, 6).Value = Round(page.PageLengthMeters);
                pagesSheet.Cell(row, 6).Style.NumberFormat.Format = "0.000";
                row++;
            }
        }

        workbook.SaveAs(destinationPath);
        return Task.CompletedTask;
    }

    private static double Round(double meters) =>
        Math.Round(meters, MeasurementDefaults.LengthMetersDecimalPlaces, MidpointRounding.AwayFromZero);

    private static double RoundMm(double mm) =>
        Math.Round(mm, MeasurementDefaults.MillimetersDecimalPlaces, MidpointRounding.AwayFromZero);
}
