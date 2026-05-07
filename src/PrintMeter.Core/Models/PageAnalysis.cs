namespace PrintMeter.Core.Models;

public sealed record PageAnalysis(
    int PageNumber,
    double WidthMm,
    double HeightMm,
    double LongMm,
    double ShortMm,
    string FormatLabel,
    double PageLengthMeters);
