namespace PrintMeter.Core;

/// <summary>
/// Серия форматов для полиграфии по короткой стороне (ширине) листа.
/// Полосы A1 до 610 мм и A0 до 914 мм соответствуют бывшим границам A1+/A0+,
/// но в отчёт идёт одна метка <c>A1</c> или <c>A0</c> с теми же условными длинными сторонами в прайсе.
/// </summary>
public sealed class Iso216FormatRegistry : IFormatRegistry
{
    private sealed record SizeBand(string Label, double MaxWidthMm);

    private static readonly SizeBand[] FormatBands =
    [
        new SizeBand("A4", 210),
        new SizeBand("A3", 297),
        new SizeBand("A2", 420),
        new SizeBand("A1", 610),
        new SizeBand("A0", 914),
    ];

    private readonly HashSet<string> _enabledFormats = new(
        FormatBands.Select(b => b.Label),
        StringComparer.Ordinal);

    public IReadOnlyList<string> SupportedFormats => FormatBands.Select(b => b.Label).ToArray();

    public IReadOnlyCollection<string> EnabledFormats => _enabledFormats.ToArray();

    public void SetEnabledFormats(IEnumerable<string> formats)
    {
        var requested = new HashSet<string>(formats, StringComparer.Ordinal);
        _enabledFormats.Clear();
        foreach (var band in FormatBands)
        {
            if (requested.Contains(band.Label))
            {
                _enabledFormats.Add(band.Label);
            }
        }
    }

    public string ResolveLabel(double longMm, double shortMm, double toleranceMm)
    {
        var widthMm = shortMm;
        foreach (var band in FormatBands)
        {
            if (widthMm <= band.MaxWidthMm + toleranceMm && _enabledFormats.Contains(band.Label))
            {
                return band.Label;
            }
        }

        var l = Math.Round(longMm, MeasurementDefaults.MillimetersDecimalPlaces);
        var s = Math.Round(shortMm, MeasurementDefaults.MillimetersDecimalPlaces);
        return $"Custom {l:0.##}×{s:0.##}mm";
    }
}
