using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using PrintMeter.Core;
using PrintMeter.Core.Models;

namespace PrintMeter.Export;

public sealed class CsvBatchReportExporter : IReportExporter
{
    public Task ExportAsync(
        BatchReport report,
        string destinationPath,
        ReportExportOptions options,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var culture = CultureInfo.InvariantCulture;
        var encoding = new UTF8Encoding(options.UseUtf8Bom);

        using var stream = new FileStream(
            destinationPath,
            new FileStreamOptions
            {
                Mode = FileMode.Create,
                Access = FileAccess.Write,
                Share = FileShare.None,
            });
        using var writer = new StreamWriter(stream, encoding);
        var config = new CsvConfiguration(culture)
        {
            Delimiter = options.CsvDelimiter.ToString(),
        };

        using var csv = new CsvWriter(writer, config);

        csv.WriteField("Section");
        csv.NextRecord();

        csv.WriteField("SummaryByFormat");
        csv.WriteField("Format");
        csv.WriteField("PageCount");
        csv.WriteField("LengthMeters");
        csv.NextRecord();

        foreach (var kv in report.SummaryByFormat.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            csv.WriteField("Summary");
            csv.WriteField(kv.Key);
            csv.WriteField(kv.Value.PageCount.ToString(culture));
            csv.WriteField(Round(kv.Value.LengthMeters).ToString(culture));
            csv.NextRecord();
        }

        csv.NextRecord();
        csv.WriteField("Files");
        csv.WriteField("FilePath");
        csv.WriteField("Pages");
        csv.WriteField("TotalLengthMeters");
        csv.WriteField("Error");
        csv.NextRecord();

        foreach (var file in report.Files)
        {
            csv.WriteField("File");
            csv.WriteField(file.FilePath);
            csv.WriteField(file.Pages.Count.ToString(culture));
            csv.WriteField(Round(file.TotalLengthMeters).ToString(culture));
            csv.WriteField(file.Error ?? string.Empty);
            csv.NextRecord();
        }

        writer.Flush();
        return Task.CompletedTask;
    }

    private static double Round(double meters) =>
        Math.Round(meters, MeasurementDefaults.LengthMetersDecimalPlaces, MidpointRounding.AwayFromZero);
}
