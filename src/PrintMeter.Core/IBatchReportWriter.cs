using PrintMeter.Core.Models;

namespace PrintMeter.Core;

public interface IBatchReportWriter
{
    Task WriteCsvAsync(BatchReport report, string destinationPath, ReportExportOptions options, CancellationToken cancellationToken);

    Task WriteXlsxAsync(
        BatchReport report,
        string destinationPath,
        ReportExportOptions options,
        CancellationToken cancellationToken);
}
