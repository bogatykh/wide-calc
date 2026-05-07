namespace PrintMeter.Core;

/// <summary>ISO 216 A-series sizes (long × short mm).</summary>
public sealed class Iso216FormatRegistry : IFormatRegistry
{
    private static readonly (string Code, double LongMm, double ShortMm)[] Sizes =
    [
        ("A0", 1189, 841),
        ("A1", 841, 594),
        ("A2", 594, 420),
        ("A3", 420, 297),
        ("A4", 297, 210),
        ("A5", 210, 148),
        ("A6", 148, 105),
        ("A7", 105, 74),
        ("A8", 74, 52),
        ("A9", 52, 37),
        ("A10", 37, 26),
    ];

    public string ResolveLabel(double longMm, double shortMm, double toleranceMm)
    {
        foreach (var (code, refLong, refShort) in Sizes)
        {
            if (Math.Abs(longMm - refLong) <= toleranceMm && Math.Abs(shortMm - refShort) <= toleranceMm)
            {
                return code;
            }
        }

        var l = Math.Round(longMm, MeasurementDefaults.MillimetersDecimalPlaces);
        var s = Math.Round(shortMm, MeasurementDefaults.MillimetersDecimalPlaces);
        return $"Custom {l:0.##}×{s:0.##}mm";
    }
}
