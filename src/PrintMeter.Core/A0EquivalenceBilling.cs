using PrintMeter.Core.Models;

namespace PrintMeter.Core;

/// <summary>
/// Billing "A0 equivalents": sum of physical long-edge millimeters for selected ISO formats (typically A0 + A0+),
/// divided by ISO A0 long side (1189 mm), then rounded — matches common typography pricing workflows.
/// </summary>
public static class A0EquivalenceBilling
{
    /// <summary>ISO 216 — длинная сторона формата A0 (мм).</summary>
    public const double IsoA0LongSideMillimeters = 1189;

    public enum RoundingMode
    {
        /// <summary>Вверх: неполная «доля» формата оплачивается как целый условный лист.</summary>
        Ceiling,

        /// <summary>Ближайшее целое, дробная часть 0.5 — от нуля.</summary>
        NearestAwayFromZero,
    }

    /// <returns>Сумма длинных сторон по выбранным форматам (мм); дробное число условных A0 до округления; целых после округления.</returns>
    public static (double CombinedLongMm, double RawSheetEquivalents, int BillingSheetCount) Compute(
        IReadOnlyDictionary<string, FormatAggregate> summaryByFormat,
        IReadOnlyList<string> includedFormatLabels,
        double divisorMillimeters,
        RoundingMode rounding)
    {
        if (includedFormatLabels.Count == 0 || divisorMillimeters <= 0)
        {
            return (0, 0, 0);
        }

        double sumMeters = 0;
        foreach (var label in includedFormatLabels)
        {
            if (summaryByFormat.TryGetValue(label, out var agg))
            {
                sumMeters += agg.LengthMeters;
            }
        }

        var combinedLongMm = sumMeters * 1000;
        var raw = combinedLongMm / divisorMillimeters;
        var count = rounding switch
        {
            RoundingMode.Ceiling =>
                combinedLongMm <= 0 ? 0 : (int)Math.Ceiling(raw - 1e-12),
            RoundingMode.NearestAwayFromZero => (int)Math.Round(raw, MidpointRounding.AwayFromZero),
            _ => throw new ArgumentOutOfRangeException(nameof(rounding)),
        };

        return (combinedLongMm, raw, count);
    }

    public static RoundingMode ParseRounding(string? s) =>
        s?.Trim().ToUpperInvariant() switch
        {
            null or "" or "CEILING" or "UP" => RoundingMode.Ceiling,
            "NEAREST" or "HALFUP" or "AWAYFROMZERO" => RoundingMode.NearestAwayFromZero,
            _ => RoundingMode.Ceiling,
        };
}
