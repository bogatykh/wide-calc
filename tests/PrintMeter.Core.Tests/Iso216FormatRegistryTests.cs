using FluentAssertions;
using PrintMeter.Core;
using Xunit;

namespace PrintMeter.Core.Tests;

public sealed class Iso216FormatRegistryTests
{
    private readonly Iso216FormatRegistry _registry = new();

    [Theory]
    [InlineData(297, 210, "A4")]
    [InlineData(297.5, 209.5, "A4")]
    [InlineData(420, 297, "A3")]
    public void Resolves_known_sizes_within_tolerance(double longMm, double shortMm, string expected)
    {
        _registry.ResolveLabel(longMm, shortMm, MeasurementDefaults.FormatToleranceMm).Should().Be(expected);
    }

    [Fact]
    public void Unknown_sizes_become_custom()
    {
        var label = _registry.ResolveLabel(333, 222, MeasurementDefaults.FormatToleranceMm);
        label.Should().StartWith("Custom");
    }

    [Fact]
    public void Size_outside_tolerance_becomes_custom()
    {
        var label = _registry.ResolveLabel(300.1, 210, 2.0);
        label.Should().StartWith("Custom");
    }
}
