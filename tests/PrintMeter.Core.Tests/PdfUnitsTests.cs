using FluentAssertions;
using PrintMeter.Core;
using Xunit;

namespace PrintMeter.Core.Tests;

public sealed class PdfUnitsTests
{
    [Fact]
    public void PointsToMillimeters_72pt_is_one_inch()
    {
        PdfUnits.PointsToMillimeters(72).Should().BeApproximately(25.4, 1e-9);
    }
}
