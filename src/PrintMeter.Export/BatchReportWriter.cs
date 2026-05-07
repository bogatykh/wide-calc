using PrintMeter.Core;
using PrintMeter.Core.Models;

namespace PrintMeter.Export;

public sealed class BatchReportWriter(CsvBatchReportExporter csv, XlsxBatchReportExporter xlsx) : IBatchReportWriter
{
    public Task WriteCsvAsync(
        BatchReport report,
        string destinationPath,
        ReportExportOptions options,
        CancellationToken cancellationToken) =>
        csv.ExportAsync(report, destinationPath, options, cancellationToken);

    public Task WriteXlsxAsync(
        BatchReport report,
        string destinationPath,
        CancellationToken cancellationToken) =>
        xlsx.ExportAsync(
            report,
            destinationPath,
            new ReportExportOptions(UseUtf8Bom: true, CsvDelimiter: ';'),
            cancellationToken);
}
