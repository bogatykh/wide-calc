namespace PrintMeter.Core.Models;

public sealed class FileReport
{
    public required string FilePath { get; init; }

    public IReadOnlyList<PageAnalysis> Pages { get; init; } = Array.Empty<PageAnalysis>();

    /// <summary>Aggregated length (m) per format label.</summary>
    public IReadOnlyDictionary<string, FormatAggregate> ByFormat { get; init; } =
        new Dictionary<string, FormatAggregate>(StringComparer.Ordinal);

    public double TotalLengthMeters { get; init; }

    public string? Error { get; init; }
}

public sealed record FormatAggregate(int PageCount, double LengthMeters);
