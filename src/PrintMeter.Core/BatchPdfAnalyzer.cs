using PrintMeter.Core.Models;

namespace PrintMeter.Core;

public sealed class BatchPdfAnalyzer(
    IPdfPageReader pdfPageReader,
    PageAnalysisService analysisService,
    int maxDegreeOfParallelism = 4)
{
    public async IAsyncEnumerable<FileReport> AnalyzeFilesAsync(
        IReadOnlyList<string> filePaths,
        IProgress<BatchProgress>? progress,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken,
        double toleranceMm = MeasurementDefaults.FormatToleranceMm)
    {
        var total = filePaths.Count;
        var completed = 0;
        using var gate = new SemaphoreSlim(maxDegreeOfParallelism, maxDegreeOfParallelism);

        async Task<FileReport> ProcessOneAsync(string path, CancellationToken ct)
        {
            await gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                progress?.Report(new BatchProgress(completed, total, path));
                try
                {
                    var pages = await pdfPageReader
                        .ReadPageDimensionsAsync(path, ct)
                        .ConfigureAwait(false);
                    var report = analysisService.BuildFileReport(path, pages, toleranceMm);
                    var done = Interlocked.Increment(ref completed);
                    progress?.Report(new BatchProgress(done, total, path));
                    return report;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    var done = Interlocked.Increment(ref completed);
                    progress?.Report(new BatchProgress(done, total, path));
                    return new FileReport
                    {
                        FilePath = path,
                        Error = ex.Message,
                        TotalLengthMeters = 0,
                    };
                }
            }
            finally
            {
                gate.Release();
            }
        }

        var tasks = new List<Task<FileReport>>(filePaths.Count);
        for (var i = 0; i < filePaths.Count; i++)
        {
            var path = filePaths[i];
            tasks.Add(ProcessOneAsync(path, cancellationToken));
        }

        while (tasks.Count > 0)
        {
            var completedTask = await Task.WhenAny(tasks).ConfigureAwait(false);
            tasks.Remove(completedTask);
            yield return await completedTask.ConfigureAwait(false);
        }
    }
}
