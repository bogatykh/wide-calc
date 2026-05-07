using FluentAssertions;
using PrintMeter.Core;
using PrintMeter.Core.Models;
using Xunit;

namespace PrintMeter.Core.Tests;

public sealed class PageAnalysisServiceTests
{
    private readonly PageAnalysisService _service = new(new Iso216FormatRegistry());

    [Fact]
    public void A4_page_in_points_is_recognized_and_length_uses_long_side_meters()
    {
        var page = new PageDimensions(1, 595, 842);
        var report = _service.BuildFileReport(@"C:\test\file.pdf", new[] { page }, MeasurementDefaults.FormatToleranceMm);

        report.Pages.Should().ContainSingle();
        report.Pages[0].FormatLabel.Should().Be("A4");
        report.TotalLengthMeters.Should().BeApproximately(0.297, 0.002);
    }

    [Fact]
    public void Combine_aggregates_formats_across_files()
    {
        var a = new FileReport
        {
            FilePath = "a.pdf",
            Pages = Array.Empty<PageAnalysis>(),
            ByFormat = new Dictionary<string, FormatAggregate>(StringComparer.Ordinal)
            {
                ["A4"] = new FormatAggregate(2, 0.6),
            },
            TotalLengthMeters = 0.6,
        };

        var b = new FileReport
        {
            FilePath = "b.pdf",
            Pages = Array.Empty<PageAnalysis>(),
            ByFormat = new Dictionary<string, FormatAggregate>(StringComparer.Ordinal)
            {
                ["A4"] = new FormatAggregate(1, 0.3),
            },
            TotalLengthMeters = 0.3,
        };

        var batch = PageAnalysisService.Combine(new[] { a, b });
        batch.SummaryByFormat["A4"].PageCount.Should().Be(3);
        batch.SummaryByFormat["A4"].LengthMeters.Should().BeApproximately(0.9, 1e-9);
        batch.TotalLengthMeters.Should().BeApproximately(0.9, 1e-9);
    }

    [Fact]
    public void Combine_skips_files_with_errors()
    {
        var ok = new FileReport
        {
            FilePath = "ok.pdf",
            Pages = Array.Empty<PageAnalysis>(),
            ByFormat = new Dictionary<string, FormatAggregate>(StringComparer.Ordinal)
            {
                ["A4"] = new FormatAggregate(1, 0.3),
            },
            TotalLengthMeters = 0.3,
        };
        var bad = new FileReport
        {
            FilePath = "bad.pdf",
            Error = "damaged",
            TotalLengthMeters = 100,
        };

        var batch = PageAnalysisService.Combine(new[] { bad, ok });
        batch.TotalLengthMeters.Should().BeApproximately(0.3, 1e-9);
        batch.SummaryByFormat.Should().ContainKey("A4");
    }
}
