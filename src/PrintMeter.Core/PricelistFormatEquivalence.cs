using PrintMeter.Core.Models;

namespace PrintMeter.Core;

/// <summary>
/// Прайс: для каждой метки формата суммируется физический метраж по длинной стороне (из сводки),
/// переводится в мм, затем делится на заданный для этого формата знаменатель (номинальная длинная сторона базового листа, мм).
/// </summary>
public static class PricelistFormatEquivalence
{
    /// <summary>Как считать целое число условных листов из дроби (договорённость офиса, не физика формата).</summary>
    public static RoundingMode DefaultRounding { get; } = RoundingMode.Ceiling;

    public enum RoundingMode
    {
        Ceiling,
        NearestAwayFromZero,
    }

    /// <summary>
    /// Номинальная длинная сторона (мм) для строки прайса. Классификация A1/A0 охватывает бывшие A1+/A0+ по ширине,
    /// но здесь считаются стандартные длины базовых форматов.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, double> IsoNominalLongEdgeMm = new Dictionary<string, double>(
        StringComparer.Ordinal)
    {
        ["A4"] = 297,
        ["A3"] = 420,
        ["A2"] = 594,
        ["A1"] = 841,
        ["A0"] = 1189,
    };

    public sealed record FormatRow(string FormatLabel, double CombinedLongMm, double DivisorMm, double RawSheets, int BillingSheets);

    public static RoundingMode ParseRounding(string? s) =>
        s?.Trim().ToUpperInvariant() switch
        {
            null or "" or "CEILING" or "UP" => RoundingMode.Ceiling,
            "NEAREST" or "HALFUP" or "AWAYFROMZERO" => RoundingMode.NearestAwayFromZero,
            _ => RoundingMode.Ceiling,
        };

    /// <summary>
    /// Собирает знаменатель по метке: сначала <paramref name="overridesMm"/> (appsettings),
    /// иначе <see cref="IsoNominalLongEdgeMm"/>; для Custom-* и неизвестных — null (строку в экспорт не берём).
    /// </summary>
    public static IReadOnlyList<FormatRow> ComputeRows(
        IReadOnlyDictionary<string, FormatAggregate> summaryByFormat,
        IReadOnlyDictionary<string, double>? overridesMm,
        RoundingMode rounding)
    {
        static double? ResolveDivisor(string label, IReadOnlyDictionary<string, double>? ov)
        {
            if (ov is not null && ov.TryGetValue(label, out var o) && o > 0)
            {
                return o;
            }

            return IsoNominalLongEdgeMm.TryGetValue(label, out var d) ? d : null;
        }

        List<FormatRow> rows = new();
        foreach (var kv in summaryByFormat.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            var label = kv.Key;
            var agg = kv.Value;
            if (agg.LengthMeters <= 0 || agg.PageCount <= 0)
            {
                continue;
            }

            var resolvedDivisorMm = ResolveDivisor(label, overridesMm);
            if (resolvedDivisorMm is null || resolvedDivisorMm <= 0)
            {
                continue;
            }

            var dm = resolvedDivisorMm.Value;
            var combinedMm = agg.LengthMeters * 1000;
            var raw = combinedMm / dm;
            var billing = rounding switch
            {
                RoundingMode.Ceiling =>
                    combinedMm <= 0 ? 0 : (int)Math.Ceiling(raw - 1e-12),
                RoundingMode.NearestAwayFromZero => (int)Math.Round(raw, MidpointRounding.AwayFromZero),
                _ => throw new ArgumentOutOfRangeException(nameof(rounding)),
            };

            rows.Add(new FormatRow(label, combinedMm, dm, raw, billing));
        }

        return rows;
    }
}
