namespace PrintMeter.Core;

/// <summary>Maps normalized long/short mm to a human-readable format label.</summary>
public interface IFormatRegistry
{
    string ResolveLabel(double longMm, double shortMm, double toleranceMm);
}
