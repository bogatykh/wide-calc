using FluentAssertions;
using PrintMeter.Core;
using Xunit;

namespace PrintMeter.Core.Tests;

public sealed class Iso216FormatRegistryTests
{
    private readonly Iso216FormatRegistry _registry = new();

    [Fact]
    public void SupportedFormats_lists_all_iso_bands_in_width_order()
    {
        _registry.SupportedFormats.Should().Equal("A4", "A3", "A2", "A1", "A0");
    }

    [Fact]
    public void SetEnabledFormats_ignores_unknown_labels()
    {
        _registry.SetEnabledFormats(["A4", "not-a-format", "A0"]);
        _registry.EnabledFormats.Should().BeEquivalentTo(new[] { "A4", "A0" });
    }

    [Fact]
    public void With_no_formats_enabled_resolution_is_always_custom()
    {
        _registry.SetEnabledFormats(Array.Empty<string>());
        _registry.ResolveLabel(297, 210, MeasurementDefaults.FormatToleranceMm).Should().StartWith("Custom");
    }

    [Theory]
    [InlineData(1200, 297, "A3")]
    [InlineData(1200, 420, "A2")]
    [InlineData(1200, 610, "A1")]
    [InlineData(1200, 910, "A0")]
    public void Groups_sizes_by_width_band_for_lekala_profile(double longMm, double shortMm, string expected)
    {
        _registry.ResolveLabel(longMm, shortMm, MeasurementDefaults.FormatToleranceMm).Should().Be(expected);
    }

    [Fact]
    public void Distinguishes_a1_and_a0()
    {
        _registry.ResolveLabel(1200, 594, MeasurementDefaults.FormatToleranceMm).Should().Be("A1");
        _registry.ResolveLabel(1200, 841, MeasurementDefaults.FormatToleranceMm).Should().Be("A0");
    }

    [Fact]
    public void Unknown_sizes_become_custom()
    {
        var label = _registry.ResolveLabel(1200, 1000, MeasurementDefaults.FormatToleranceMm);
        label.Should().StartWith("Custom");
    }

    [Fact]
    public void With_only_smaller_formats_enabled_wide_sheet_is_custom_not_a_plus_bucket()
    {
        _registry.SetEnabledFormats(["A4", "A3", "A2", "A1"]);
        _registry.ResolveLabel(1200, 841, MeasurementDefaults.FormatToleranceMm).Should().StartWith("Custom");
    }
}
