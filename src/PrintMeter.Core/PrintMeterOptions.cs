namespace PrintMeter.Core;

public sealed class PrintMeterOptions
{
    public const string SectionName = "PrintMeter";

    public double FormatToleranceMm { get; set; } = MeasurementDefaults.FormatToleranceMm;

    public int MaxDegreeOfParallelism { get; set; } = 4;

    /// <summary>Знаменатель для прайсового «условного A0» (мм длинной стороны): по умолчанию ISO A0 = 1189.</summary>
    public double A0EquivalenceDivisorMm { get; set; } = A0EquivalenceBilling.IsoA0LongSideMillimeters;

    /// <summary>Суммируются длинные стороны (мм) по этим ISO-меткам; по умолчанию A0 и A0+.</summary>
    public string[] A0EquivalenceIncludedFormats { get; set; } = ["A0", "A0+"];

    /// <summary>Ceiling (вверх) или Nearest (к ближайшему).</summary>
    public string A0EquivalenceRounding { get; set; } = "Ceiling";
}
