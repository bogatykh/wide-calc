using PrintMeter.Core.Models;

namespace PrintMeter.Core;

public interface IReportExporter
{
    Task ExportAsync(BatchReport report, string destinationPath, ReportExportOptions options, CancellationToken cancellationToken);
}

public sealed record ReportExportOptions(bool UseUtf8Bom, char CsvDelimiter);
