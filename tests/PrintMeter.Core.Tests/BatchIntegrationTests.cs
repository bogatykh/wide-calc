using FluentAssertions;
using PrintMeter.Core;
using PrintMeter.Core.Models;
using Xunit;

namespace PrintMeter.Core.Tests;

public sealed class BatchIntegrationTests
{
    private sealed class MixedFormatReader : IPdfPageReader
    {
        public Task<IReadOnlyList<PageDimensions>> ReadPageDimensionsAsync(string filePath, CancellationToken cancellationToken)
        {
            var pages = filePath switch
            {
                "job-1.pdf" => new[]
                {
                    // A4
                    new PageDimensions(1, 595, 842),
                    // A3
                    new PageDimensions(2, 842, 1191),
                },
                "job-2.pdf" => new[]
                {
                    // A4 landscape
                    new PageDimensions(1, 842, 595),
                    // custom
                    new PageDimensions(2, 700, 700),
                },
                _ => Array.Empty<PageDimensions>(),
            };

            return Task.FromResult<IReadOnlyList<PageDimensions>>(pages);
        }
    }

    [Fact]
    public async Task Mixed_formats_produce_expected_batch_summary()
    {
        var analyzer = new BatchPdfAnalyzer(
            new MixedFormatReader(),
            new PageAnalysisService(new Iso216FormatRegistry()),
            maxDegreeOfParallelism: 2);

        var fileReports = new List<FileReport>();
        await foreach (var report in analyzer.AnalyzeFilesAsync(
                           new[] { "job-1.pdf", "job-2.pdf" },
                           progress: null,
                           cancellationToken: CancellationToken.None))
        {
            fileReports.Add(report);
        }

        var batch = PageAnalysisService.Combine(fileReports);

        batch.Files.Should().HaveCount(2);
        batch.SummaryByFormat.Should().ContainKey("A4");
        batch.SummaryByFormat["A4"].PageCount.Should().Be(2);
        batch.SummaryByFormat.Should().ContainKey("A3");
        batch.SummaryByFormat["A3"].PageCount.Should().Be(1);
        batch.SummaryByFormat.Keys.Should().Contain(k => k.StartsWith("Custom ", StringComparison.Ordinal));
        batch.TotalLengthMeters.Should().BeGreaterThan(1.0);
    }
}
