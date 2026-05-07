using FluentAssertions;
using PrintMeter.Core;
using PrintMeter.Core.Models;
using Xunit;

namespace PrintMeter.Export.Tests;

public sealed class CsvBatchReportExporterTests
{
    [Fact]
    public async Task Writes_utf8_bom_and_semicolon_delimiter()
    {
        var report = new BatchReport
        {
            Files = new[]
            {
                new FileReport
                {
                    FilePath = "a.pdf",
                    Pages = Array.Empty<PageAnalysis>(),
                    ByFormat = new Dictionary<string, FormatAggregate>(StringComparer.Ordinal)
                    {
                        ["A4"] = new FormatAggregate(1, 0.297),
                    },
                    TotalLengthMeters = 0.297,
                },
            },
            SummaryByFormat = new Dictionary<string, FormatAggregate>(StringComparer.Ordinal)
            {
                ["A4"] = new FormatAggregate(1, 0.297),
            },
            TotalLengthMeters = 0.297,
        };

        var path = Path.Combine(Path.GetTempPath(), $"printmeter-{Guid.NewGuid():N}.csv");
        try
        {
            var exporter = new CsvBatchReportExporter();
            await exporter.ExportAsync(
                report,
                path,
                new ReportExportOptions(UseUtf8Bom: true, CsvDelimiter: ';'),
                CancellationToken.None);

            var bytes = await File.ReadAllBytesAsync(path);
            bytes[0].Should().Be(0xEF);
            bytes[1].Should().Be(0xBB);
            bytes[2].Should().Be(0xBF);

            var text = await File.ReadAllTextAsync(path);
            text.Should().Contain(";");
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
    public async Task Writes_without_bom_when_disabled()
    {
        var report = new BatchReport
        {
            Files = Array.Empty<FileReport>(),
            SummaryByFormat = new Dictionary<string, FormatAggregate>(StringComparer.Ordinal),
            TotalLengthMeters = 0,
        };

        var path = Path.Combine(Path.GetTempPath(), $"printmeter-{Guid.NewGuid():N}.csv");
        try
        {
            var exporter = new CsvBatchReportExporter();
            await exporter.ExportAsync(
                report,
                path,
                new ReportExportOptions(UseUtf8Bom: false, CsvDelimiter: ';'),
                CancellationToken.None);

            var bytes = await File.ReadAllBytesAsync(path);
            bytes.Length.Should().BeGreaterThan(0);
            bytes[0].Should().NotBe(0xEF);
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
