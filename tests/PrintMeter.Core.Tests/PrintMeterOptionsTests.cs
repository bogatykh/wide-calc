using FluentAssertions;
using Xunit;

namespace PrintMeter.Core.Tests;

public sealed class PrintMeterOptionsTests
{
    [Fact]
    public void Defaults_match_measurement_and_parallelism_contract()
    {
        var o = new PrintMeterOptions();
        o.FormatToleranceMm.Should().Be(MeasurementDefaults.FormatToleranceMm);
        o.MaxDegreeOfParallelism.Should().Be(4);
    }

    [Fact]
    public void SectionName_matches_configuration_key()
    {
        PrintMeterOptions.SectionName.Should().Be("PrintMeter");
    }
}
