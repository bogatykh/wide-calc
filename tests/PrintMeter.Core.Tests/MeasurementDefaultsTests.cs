using FluentAssertions;
using Xunit;

namespace PrintMeter.Core.Tests;

public sealed class MeasurementDefaultsTests
{
    [Fact]
    public void PageLengthMeters_uses_long_edge_regardless_of_orientation()
    {
        var portrait = MeasurementDefaults.PageLengthMeters(210, 297);
        var landscape = MeasurementDefaults.PageLengthMeters(297, 210);

        portrait.Should().BeApproximately(0.297, 1e-12);
        landscape.Should().BeApproximately(0.297, 1e-12);
    }

    [Fact]
    public void PageLengthMeters_square_sheet_uses_side_length()
    {
        MeasurementDefaults.PageLengthMeters(300, 300).Should().Be(0.3);
    }
}
