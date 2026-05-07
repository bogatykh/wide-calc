using PrintMeter.Core.Models;

namespace PrintMeter.Core;

public interface IReportExporter
{
    Task ExportAsync(BatchReport report, string destinationPath, ReportExportOptions options, CancellationToken cancellationToken);
}

public sealed record ReportExportOptions(
    bool UseUtf8Bom,
    char CsvDelimiter,
    A0BillingExportSnapshot? BillingA0 = null);

/// <summary>Строки «условного A0» для прайса (для блока экспорта).</summary>
public sealed record A0BillingExportSnapshot(
    double CombinedLongMm,
    double DivisorMm,
    double RawSheetEquivalents,
    int BillingSheetCount,
    string IncludedFormats,
    string RoundingModeKey);
