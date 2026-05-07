namespace PrintMeter.Core.Models;

public sealed class BatchReport
{
    public IReadOnlyList<FileReport> Files { get; init; } = Array.Empty<FileReport>();

    public IReadOnlyDictionary<string, FormatAggregate> SummaryByFormat { get; init; } =
        new Dictionary<string, FormatAggregate>(StringComparer.Ordinal);

    public double TotalLengthMeters { get; init; }
}
