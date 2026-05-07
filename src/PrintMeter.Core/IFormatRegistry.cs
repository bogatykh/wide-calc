namespace PrintMeter.Core;

/// <summary>Maps normalized long/short mm to a human-readable format label.</summary>
public interface IFormatRegistry
{
    IReadOnlyList<string> SupportedFormats { get; }
    IReadOnlyCollection<string> EnabledFormats { get; }
    void SetEnabledFormats(IEnumerable<string> formats);
    string ResolveLabel(double longMm, double shortMm, double toleranceMm);
}
