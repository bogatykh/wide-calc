using FluentAssertions;
using PrintMeter.Core;
using PrintMeter.Core.Models;
using Xunit;

namespace PrintMeter.Core.Tests;

public sealed class BatchPdfAnalyzerTests
{
    private sealed class FakeReader(Func<string, IReadOnlyList<PageDimensions>> factory) : IPdfPageReader
    {
        public Task<IReadOnlyList<PageDimensions>> ReadPageDimensionsAsync(string filePath, CancellationToken cancellationToken) =>
            Task.FromResult(factory(filePath));
    }

    [Fact]
    public async Task Yields_all_results()
    {
        var reader = new FakeReader(
            path => path switch
            {
                "a" => new[] { new PageDimensions(1, 595, 842) },
                "b" => new[] { new PageDimensions(1, 420, 595) },
                _ => Array.Empty<PageDimensions>(),
            });

        var analyzer = new BatchPdfAnalyzer(reader, new PageAnalysisService(new Iso216FormatRegistry()), 2);
        var results = new List<FileReport>();
        await foreach (var r in analyzer.AnalyzeFilesAsync(new[] { "a", "b" }, null, CancellationToken.None))
        {
            results.Add(r);
        }

        results.Should().HaveCount(2);
        results.Select(r => r.FilePath).Should().BeEquivalentTo(new[] { "a", "b" });
    }

    [Fact]
    public async Task Errors_do_not_stop_batch()
    {
        var reader = new FakeReader(
            path => path == "bad" ? throw new InvalidOperationException("boom") : new[] { new PageDimensions(1, 595, 842) });

        var analyzer = new BatchPdfAnalyzer(reader, new PageAnalysisService(new Iso216FormatRegistry()), 2);
        var results = new List<FileReport>();
        await foreach (var r in analyzer.AnalyzeFilesAsync(new[] { "bad", "good" }, null, CancellationToken.None))
        {
            results.Add(r);
        }

        results[0].Error.Should().NotBeNullOrEmpty();
        results[1].Error.Should().BeNull();
    }

    [Fact]
    public async Task Reports_progress_for_each_file()
    {
        var reader = new FakeReader(_ => new[] { new PageDimensions(1, 595, 842) });
        var analyzer = new BatchPdfAnalyzer(reader, new PageAnalysisService(new Iso216FormatRegistry()), 2);
        var progress = new List<BatchProgress>();
        var sync = new object();
        var reporter = new Progress<BatchProgress>(
            p =>
            {
                lock (sync)
                {
                    progress.Add(p);
                }
            });

        var results = new List<FileReport>();
        await foreach (var r in analyzer.AnalyzeFilesAsync(new[] { "a", "b" }, reporter, CancellationToken.None))
        {
            results.Add(r);
        }

        results.Should().HaveCount(2);
        lock (sync)
        {
            progress.Should().NotBeEmpty();
            progress.Should().Contain(p => p.CompletedFiles == 2 && p.TotalFiles == 2);
        }
    }

    [Fact]
    public async Task Canceled_token_throws_operation_canceled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var reader = new FakeReader(_ => new[] { new PageDimensions(1, 595, 842) });
        var analyzer = new BatchPdfAnalyzer(reader, new PageAnalysisService(new Iso216FormatRegistry()), 1);

        var act = async () =>
        {
            await foreach (var _ in analyzer.AnalyzeFilesAsync(new[] { "a" }, null, cts.Token))
            {
            }
        };

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
