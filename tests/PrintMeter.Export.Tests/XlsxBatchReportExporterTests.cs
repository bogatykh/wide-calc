using ClosedXML.Excel;
using FluentAssertions;
using PrintMeter.Core;
using PrintMeter.Core.Models;
using Xunit;

namespace PrintMeter.Export.Tests;

public sealed class XlsxBatchReportExporterTests
{
    [Fact]
    public async Task Writes_summary_sheet()
    {
        var report = new BatchReport
        {
            Files = Array.Empty<FileReport>(),
            SummaryByFormat = new Dictionary<string, FormatAggregate>(StringComparer.Ordinal)
            {
                ["A4"] = new FormatAggregate(2, 0.594),
            },
            TotalLengthMeters = 0.594,
        };

        var path = Path.Combine(Path.GetTempPath(), $"printmeter-{Guid.NewGuid():N}.xlsx");
        try
        {
            var exporter = new XlsxBatchReportExporter();
            await exporter.ExportAsync(
                report,
                path,
                new ReportExportOptions(UseUtf8Bom: true, CsvDelimiter: ';'),
                CancellationToken.None);

            using var wb = new XLWorkbook(path);
            var sheet = wb.Worksheet("Summary");
            sheet.Cell(2, 1).GetString().Should().Be("A4");
            sheet.Cell(2, 2).GetDouble().Should().BeApproximately(2, 1e-9);
            wb.Worksheets.Select(w => w.Name).Should().Contain(new[] { "Summary", "Files", "Pages" });
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
