namespace PrintMeter.Core;

/// <summary>
/// Default analysis parameters (tolerance and rounding). Adjust via appsettings in a future version.
/// </summary>
public static class MeasurementDefaults
{
    /// <summary>± tolerance when matching ISO A-series physical sizes (mm).</summary>
    public const double FormatToleranceMm = 2.0;

    /// <summary>Decimal places for length in meters in exported reports.</summary>
    public const int LengthMetersDecimalPlaces = 3;

    /// <summary>Decimal places for millimeter dimensions in exports.</summary>
    public const int MillimetersDecimalPlaces = 2;

    /// <summary>
    /// Per-page contribution to linear run length (meters).
    /// Uses the long side of the sheet in mm / 1000 (matches portrait "height" for A-series).
    /// </summary>
    public static double PageLengthMeters(double widthMm, double heightMm) =>
        Math.Max(widthMm, heightMm) / 1000.0;
}
