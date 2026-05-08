using System.Globalization;
using System.Text;
using PrintMeter.Core;
using PrintMeter.Core.Models;

namespace PrintMeter.App;

/// <summary>
/// Lightweight in-app exporter to keep WinUI app independent from heavy export dependencies during XAML compilation in CI.
/// </summary>
public sealed class LightweightBatchReportWriter : IBatchReportWriter
{
    public async Task WriteCsvAsync(
        BatchReport report,
        string destinationPath,
        ReportExportOptions options,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var culture = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        var d = options.CsvDelimiter;

        sb.Append("Section").AppendLine();
        sb.Append("SummaryByFormat").Append(d).Append("Format").Append(d).Append("PageCount").Append(d).Append("LengthMeters").AppendLine();
        foreach (var kv in report.SummaryByFormat.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            sb.Append("Summary")
                .Append(d).Append(kv.Key)
                .Append(d).Append(kv.Value.PageCount.ToString(culture))
                .Append(d).Append(kv.Value.LengthMeters.ToString("0.000", culture))
                .AppendLine();
        }

        sb.AppendLine()
            .Append("Details").Append(d).Append("FilePath").Append(d).Append("PageCount").Append(d).Append("LengthMeters").Append(d).Append("Error").AppendLine();
        foreach (var file in report.Files)
        {
            sb.Append("File")
                .Append(d).Append(Escape(file.FilePath, d))
                .Append(d).Append(file.Pages.Count.ToString(culture))
                .Append(d).Append(file.TotalLengthMeters.ToString("0.000", culture))
                .Append(d).Append(Escape(file.Error ?? string.Empty, d))
                .AppendLine();
        }

        var encoding = new UTF8Encoding(options.UseUtf8Bom);
        await File.WriteAllTextAsync(destinationPath, sb.ToString(), encoding, cancellationToken).ConfigureAwait(false);
    }

    public Task WriteXlsxAsync(
        BatchReport report,
        string destinationPath,
        ReportExportOptions options,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException("XLSX export is temporarily disabled in this CI-safe build profile.");
    }

    private static string Escape(string value, char delimiter)
    {
        if (value.Contains(delimiter) || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        return value;
    }
}
