using CommunityToolkit.Mvvm.Input;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PrintMeter.Core;
using PrintMeter.Core.Models;
using Xunit;

namespace PrintMeter.App.ViewModels.Tests;

public sealed class MainViewModelTests
{
    private sealed class FakePdfPageReader(Func<string, IReadOnlyList<PageDimensions>> factory) : IPdfPageReader
    {
        public Task<IReadOnlyList<PageDimensions>> ReadPageDimensionsAsync(string filePath, CancellationToken cancellationToken) =>
            Task.FromResult(factory(filePath));
    }

    private sealed class DelayedPdfPageReader(TimeSpan delay) : IPdfPageReader
    {
        public async Task<IReadOnlyList<PageDimensions>> ReadPageDimensionsAsync(string filePath, CancellationToken cancellationToken)
        {
            await Task.Delay(delay, cancellationToken);
            return new[] { new PageDimensions(1, 595, 842) };
        }
    }

    private sealed class RecordingDialogs : IFileDialogService
    {
        public IReadOnlyList<string>? PickFilesResult { get; set; }

        public string? PickFolderResult { get; set; }

        public Task<IReadOnlyList<string>?> PickPdfFilesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(PickFilesResult);

        public Task<string?> PickFolderAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(PickFolderResult);
    }

    [Fact]
    public async Task Pick_files_then_analyze_populates_rows()
    {
        var reader = new FakePdfPageReader(
            path => path.EndsWith("a.pdf", StringComparison.OrdinalIgnoreCase)
                ? new[] { new PageDimensions(1, 595, 842) }
                : Array.Empty<PageDimensions>());

        var analyzer = new BatchPdfAnalyzer(reader, new PageAnalysisService(new Iso216FormatRegistry()), 2);
        var dialogs = new RecordingDialogs { PickFilesResult = new[] { @"C:\demo\a.pdf" } };
        var options = Options.Create(new PrintMeterOptions());
        var vm = new MainViewModel(new Iso216FormatRegistry(), analyzer, dialogs, options, NullLogger<MainViewModel>.Instance);

        await ((IAsyncRelayCommand)vm.PickFilesCommand).ExecuteAsync(null);
        await ((IAsyncRelayCommand)vm.AnalyzeCommand).ExecuteAsync(null);

        vm.Rows.Should().ContainSingle();
        vm.Rows[0].FormatsSummary.Should().Contain("A4");
        vm.Rows[0].PageCount.Should().Be(1);
        vm.TotalLengthMeters.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Analyze_handles_reader_error_in_row()
    {
        var reader = new FakePdfPageReader(_ => throw new InvalidOperationException("bad pdf"));
        var analyzer = new BatchPdfAnalyzer(reader, new PageAnalysisService(new Iso216FormatRegistry()), 1);
        var vm = new MainViewModel(
            new Iso216FormatRegistry(),
            analyzer,
            new RecordingDialogs { PickFilesResult = new[] { @"C:\demo\bad.pdf" } },
            Options.Create(new PrintMeterOptions()),
            NullLogger<MainViewModel>.Instance);

        await ((IAsyncRelayCommand)vm.PickFilesCommand).ExecuteAsync(null);
        await ((IAsyncRelayCommand)vm.AnalyzeCommand).ExecuteAsync(null);

        vm.Rows.Should().ContainSingle();
        vm.Rows[0].Error.Should().Contain("bad pdf");
    }

    [Fact]
    public async Task AnalyzeCommand_disabled_when_no_files_selected()
    {
        var vm = new MainViewModel(
            new Iso216FormatRegistry(),
            new BatchPdfAnalyzer(
                new FakePdfPageReader(_ => Array.Empty<PageDimensions>()),
                new PageAnalysisService(new Iso216FormatRegistry()),
                1),
            new RecordingDialogs { PickFilesResult = null },
            Options.Create(new PrintMeterOptions()),
            NullLogger<MainViewModel>.Instance);

        await ((IAsyncRelayCommand)vm.PickFilesCommand).ExecuteAsync(null);

        vm.AnalyzeCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task CancelCommand_toggles_while_analysis_running()
    {
        var vm = new MainViewModel(
            new Iso216FormatRegistry(),
            new BatchPdfAnalyzer(
                new DelayedPdfPageReader(TimeSpan.FromMilliseconds(200)),
                new PageAnalysisService(new Iso216FormatRegistry()),
                1),
            new RecordingDialogs { PickFilesResult = new[] { @"C:\demo\slow.pdf" } },
            Options.Create(new PrintMeterOptions()),
            NullLogger<MainViewModel>.Instance);

        await ((IAsyncRelayCommand)vm.PickFilesCommand).ExecuteAsync(null);
        var runTask = ((IAsyncRelayCommand)vm.AnalyzeCommand).ExecuteAsync(null);

        var spinStart = DateTime.UtcNow;
        while (!vm.IsBusy && DateTime.UtcNow - spinStart < TimeSpan.FromSeconds(1))
        {
            await Task.Delay(10);
        }

        vm.IsBusy.Should().BeTrue();
        vm.CancelCommand.CanExecute(null).Should().BeTrue();

        vm.CancelCommand.Execute(null);
        await runTask;

        vm.IsBusy.Should().BeFalse();
        vm.CancelCommand.CanExecute(null).Should().BeFalse();
        vm.StatusText.Should().Be("Отменено.");
    }
}
