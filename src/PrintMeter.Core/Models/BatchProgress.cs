namespace PrintMeter.Core.Models;

public sealed record BatchProgress(int CompletedFiles, int TotalFiles, string? CurrentFile);
