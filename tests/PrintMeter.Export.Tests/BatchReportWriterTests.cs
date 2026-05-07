using FluentAssertions;
using PrintMeter.Core;
using PrintMeter.Core.Models;
using Xunit;

namespace PrintMeter.Export.Tests;

public sealed class BatchReportWriterTests
{
    private static BatchReport MinimalReport() =>
        new()
        {
            Files = Array.Empty<FileReport>(),
            SummaryByFormat = new Dictionary<string, FormatAggregate>(StringComparer.Ordinal)
            {
                ["A4"] = new FormatAggregate(1, 0.297),
            },
            TotalLengthMeters = 0.297,
        };

    [Fact]
    public async Task WriteCsvAsync_writes_file_via_csv_exporter()
    {
        var writer = new BatchReportWriter(new CsvBatchReportExporter(), new XlsxBatchReportExporter());
        var path = Path.Combine(Path.GetTempPath(), $"printmeter-writer-{Guid.NewGuid():N}.csv");
        try
        {
            await writer.WriteCsvAsync(
                MinimalReport(),
                path,
                new ReportExportOptions(UseUtf8Bom: false, CsvDelimiter: ';'),
                CancellationToken.None);

            File.Exists(path).Should().BeTrue();
            var text = await File.ReadAllTextAsync(path);
            text.Should().Contain("A4");
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public async Task WriteXlsxAsync_writes_file_via_xlsx_exporter()
    {
        var writer = new BatchReportWriter(new CsvBatchReportExporter(), new XlsxBatchReportExporter());
        var path = Path.Combine(Path.GetTempPath(), $"printmeter-writer-{Guid.NewGuid():N}.xlsx");
        try
        {
            await writer.WriteXlsxAsync(
                MinimalReport(),
                path,
                new ReportExportOptions(UseUtf8Bom: false, CsvDelimiter: ';'),
                CancellationToken.None);

            File.Exists(path).Should().BeTrue();
            new FileInfo(path).Length.Should().BeGreaterThan(0);
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
