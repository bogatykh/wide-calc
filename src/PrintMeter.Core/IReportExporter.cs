using PrintMeter.Core.Models;

namespace PrintMeter.Core;

public interface IReportExporter
{
    Task ExportAsync(BatchReport report, string destinationPath, ReportExportOptions options, CancellationToken cancellationToken);
}

public sealed record ReportExportOptions(
    bool UseUtf8Bom,
    char CsvDelimiter,
    PricelistEquivalenceExportAttachment? PricelistEquivalence = null);

/// <summary>Условные «страницы» прайса по каждому формату со знаменателем (экспорт).</summary>
public sealed record PricelistEquivalenceExportAttachment(
    IReadOnlyList<PricelistFormatEquivalence.FormatRow> PerFormatRows,
    string RoundingModeKey);
