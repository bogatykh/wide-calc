namespace PrintMeter.Core.Models;

/// <summary>Physical page size in PDF user space (points), 1-based page index.</summary>
public sealed record PageDimensions(int PageNumber, double WidthPt, double HeightPt);
