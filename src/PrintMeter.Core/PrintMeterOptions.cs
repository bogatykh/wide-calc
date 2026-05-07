namespace PrintMeter.Core;

public sealed class PrintMeterOptions
{
    public const string SectionName = "PrintMeter";

    public double FormatToleranceMm { get; set; } = MeasurementDefaults.FormatToleranceMm;

    public int MaxDegreeOfParallelism { get; set; } = 4;
}
