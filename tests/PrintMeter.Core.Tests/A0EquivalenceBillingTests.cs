using FluentAssertions;
using PrintMeter.Core.Models;
using Xunit;

namespace PrintMeter.Core.Tests;

public sealed class A0EquivalenceBillingTests
{
    [Fact]
    public void Compute_ceiling_counts_partial_as_full_sheet()
    {
        var summary = new Dictionary<string, FormatAggregate>(StringComparer.Ordinal)
        {
            // Один условный A0+ без полного набора миллиметров на целое A0
            ["A0+"] = new FormatAggregate(1, 1100 / 1000.0),
        };

        var (mm, raw, sheets) = A0EquivalenceBilling.Compute(
            summary,
            ["A0", "A0+"],
            A0EquivalenceBilling.IsoA0LongSideMillimeters,
            A0EquivalenceBilling.RoundingMode.Ceiling);

        mm.Should().BeApproximately(1100, 1e-6);
        raw.Should().BeLessThan(1.0).And.BeGreaterThan(0);
        sheets.Should().Be(1);
    }

    [Fact]
    public void Combined_A0_Line_plus_A0Plus_matches_sum_of_LengthMeters_in_mm()
    {
        var summary = new Dictionary<string, FormatAggregate>(StringComparer.Ordinal)
        {
            ["A4"] = new FormatAggregate(1, 0.297),
            ["A0"] = new FormatAggregate(3, 3 * (1189 / 1000.0)),
            ["A0+"] = new FormatAggregate(1, 1292 / 1000.0),
        };

        var (mm, raw, _) = A0EquivalenceBilling.Compute(
            summary,
            ["A0", "A0+"],
            A0EquivalenceBilling.IsoA0LongSideMillimeters,
            A0EquivalenceBilling.RoundingMode.Ceiling);

        mm.Should().BeApproximately((3 * 1189 + 1292), 5);
        raw.Should().BeApproximately(mm / A0EquivalenceBilling.IsoA0LongSideMillimeters, 1e-6);
    }

    [Fact]
    public void Nearest_below_half_rounds_down_Ceiling_still_counts_one_sheet_when_nonzero_mm()
    {
        // ~300 мм < ½·1189: ближайшее целое число условных листов = 0, потолок = 1
        var summary = new Dictionary<string, FormatAggregate>(StringComparer.Ordinal)
        {
            ["A0"] = new FormatAggregate(1, 0.3),
        };

        A0EquivalenceBilling.Compute(
                summary,
                ["A0"],
                A0EquivalenceBilling.IsoA0LongSideMillimeters,
                A0EquivalenceBilling.RoundingMode.NearestAwayFromZero)
            .Item3.Should().Be(0);

        A0EquivalenceBilling.Compute(
                summary,
                ["A0"],
                A0EquivalenceBilling.IsoA0LongSideMillimeters,
                A0EquivalenceBilling.RoundingMode.Ceiling)
            .Item3.Should().Be(1);
    }
}
