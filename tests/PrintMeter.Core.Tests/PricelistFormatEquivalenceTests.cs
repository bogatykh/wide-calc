using FluentAssertions;
using PrintMeter.Core.Models;
using Xunit;

namespace PrintMeter.Core.Tests;

public sealed class PricelistFormatEquivalenceTests
{
    [Fact]
    public void Iso_row_uses_built_in_divisor_ceiling_partial_counts_as_full()
    {
        var summary = new Dictionary<string, FormatAggregate>(StringComparer.Ordinal)
        {
            ["A0+"] = new FormatAggregate(1, 1100 / 1000.0),
        };

        var rows = PricelistFormatEquivalence.ComputeRows(
            summary,
            overridesMm: null,
            PricelistFormatEquivalence.RoundingMode.Ceiling);

        rows.Should().ContainSingle();
        var r = rows[0];
        r.FormatLabel.Should().Be("A0+");
        r.DivisorMm.Should().Be(PricelistFormatEquivalence.IsoNominalLongEdgeMm["A0+"]);
        r.CombinedLongMm.Should().BeApproximately(1100, 1e-6);
        r.BillingSheets.Should().Be(1);
    }

    [Fact]
    public void Multiple_formats_ordered_by_label_with_separate_quotients()
    {
        var summary = new Dictionary<string, FormatAggregate>(StringComparer.Ordinal)
        {
            ["A3"] = new FormatAggregate(2, 2 * (420 / 1000.0)),
            ["A0"] = new FormatAggregate(1, 1189 / 1000.0),
        };

        var rows = PricelistFormatEquivalence.ComputeRows(
            summary,
            null,
            PricelistFormatEquivalence.RoundingMode.Ceiling);

        rows.Should().HaveCount(2);
        rows[0].FormatLabel.Should().Be("A0");
        rows[0].BillingSheets.Should().Be(1);
        rows[1].FormatLabel.Should().Be("A3");
        rows[1].RawSheets.Should().BeApproximately(2.0, 1e-9);
        rows[1].BillingSheets.Should().Be(2);
    }

    [Fact]
    public void Custom_label_skipped_until_explicit_override()
    {
        var summary = new Dictionary<string, FormatAggregate>(StringComparer.Ordinal)
        {
            ["Custom 100×200mm"] = new FormatAggregate(1, 0.2),
        };

        PricelistFormatEquivalence.ComputeRows(summary, null, PricelistFormatEquivalence.RoundingMode.Ceiling)
            .Should()
            .BeEmpty();

        PricelistFormatEquivalence.ComputeRows(
                summary,
                new Dictionary<string, double>(StringComparer.Ordinal) { ["Custom 100×200mm"] = 297 },
                PricelistFormatEquivalence.RoundingMode.Ceiling)
            .Should()
            .ContainSingle();
    }

    [Fact]
    public void Nearest_vs_ceiling_difference_for_small_run()
    {
        var summary = new Dictionary<string, FormatAggregate>(StringComparer.Ordinal)
        {
            ["A0"] = new FormatAggregate(1, 0.3),
        };

        PricelistFormatEquivalence.ComputeRows(summary, null, PricelistFormatEquivalence.RoundingMode.NearestAwayFromZero)
            [0]
            .BillingSheets.Should()
            .Be(0);

        PricelistFormatEquivalence.ComputeRows(summary, null, PricelistFormatEquivalence.RoundingMode.Ceiling)
            [0]
            .BillingSheets.Should()
            .Be(1);
    }
}
