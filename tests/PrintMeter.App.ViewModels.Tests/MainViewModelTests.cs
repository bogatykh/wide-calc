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

    private sealed class RecordingWriter : IBatchReportWriter
    {
        public string? LastCsvPath { get; private set; }

        public string? LastXlsxPath { get; private set; }

        public int CsvCalls { get; private set; }

        public int XlsxCalls { get; private set; }

        public Task WriteCsvAsync(BatchReport report, string destinationPath, ReportExportOptions options, CancellationToken cancellationToken)
        {
            CsvCalls++;
            LastCsvPath = destinationPath;
            return Task.CompletedTask;
        }

        public Task WriteXlsxAsync(BatchReport report, string destinationPath, CancellationToken cancellationToken)
        {
            XlsxCalls++;
            LastXlsxPath = destinationPath;
            return Task.CompletedTask;
        }
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

        public string? SaveFileResult { get; set; }

        public Task<IReadOnlyList<string>?> PickPdfFilesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(PickFilesResult);

        public Task<string?> PickFolderAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(PickFolderResult);

        public Task<string?> SaveFileAsync(string filter, string defaultFileName, CancellationToken cancellationToken = default) =>
            Task.FromResult(SaveFileResult);
    }

    [Fact]
    public async Task Pick_files_then_analyze_populates_rows()
    {
        var reader = new FakePdfPageReader(
            path => path.EndsWith("a.pdf", StringComparison.OrdinalIgnoreCase)
                ? new[] { new PageDimensions(1, 595, 842) }
                : Array.Empty<PageDimensions>());

        var analyzer = new BatchPdfAnalyzer(reader, new PageAnalysisService(new Iso216FormatRegistry()), 2);
        var writer = new RecordingWriter();
        var dialogs = new RecordingDialogs { PickFilesResult = new[] { @"C:\demo\a.pdf" } };
        var options = Options.Create(new PrintMeterOptions());
        var vm = new MainViewModel(new Iso216FormatRegistry(), analyzer, writer, dialogs, options, NullLogger<MainViewModel>.Instance);

        await ((IAsyncRelayCommand)vm.PickFilesCommand).ExecuteAsync(null);
        await ((IAsyncRelayCommand)vm.AnalyzeCommand).ExecuteAsync(null);

        vm.Rows.Should().ContainSingle();
        vm.Rows[0].FormatsSummary.Should().Contain("A4");
        vm.Rows[0].PageCount.Should().Be(1);
        vm.TotalLengthMeters.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Export_csv_invokes_writer()
    {
        var reader = new FakePdfPageReader(_ => new[] { new PageDimensions(1, 595, 842) });
        var analyzer = new BatchPdfAnalyzer(reader, new PageAnalysisService(new Iso216FormatRegistry()), 2);
        var writer = new RecordingWriter();
        var dialogs = new RecordingDialogs
        {
            PickFilesResult = new[] { @"C:\demo\a.pdf" },
            SaveFileResult = Path.Combine(Path.GetTempPath(), $"pm-{Guid.NewGuid():N}.csv"),
        };

        var options = Options.Create(new PrintMeterOptions());
        var vm = new MainViewModel(new Iso216FormatRegistry(), analyzer, writer, dialogs, options, NullLogger<MainViewModel>.Instance);

        await ((IAsyncRelayCommand)vm.PickFilesCommand).ExecuteAsync(null);
        await ((IAsyncRelayCommand)vm.AnalyzeCommand).ExecuteAsync(null);
        await ((IAsyncRelayCommand)vm.ExportCsvCommand).ExecuteAsync(null);

        writer.LastCsvPath.Should().Be(dialogs.SaveFileResult);

        if (dialogs.SaveFileResult is not null && File.Exists(dialogs.SaveFileResult))
        {
            File.Delete(dialogs.SaveFileResult);
        }
    }

    [Fact]
    public async Task Export_xlsx_invokes_writer()
    {
        var reader = new FakePdfPageReader(_ => new[] { new PageDimensions(1, 595, 842) });
        var analyzer = new BatchPdfAnalyzer(reader, new PageAnalysisService(new Iso216FormatRegistry()), 2);
        var writer = new RecordingWriter();
        var dialogs = new RecordingDialogs
        {
            PickFilesResult = new[] { @"C:\demo\a.pdf" },
            SaveFileResult = Path.Combine(Path.GetTempPath(), $"pm-{Guid.NewGuid():N}.xlsx"),
        };

        var vm = new MainViewModel(
            new Iso216FormatRegistry(),
            analyzer,
            writer,
            dialogs,
            Options.Create(new PrintMeterOptions()),
            NullLogger<MainViewModel>.Instance);

        await ((IAsyncRelayCommand)vm.PickFilesCommand).ExecuteAsync(null);
        await ((IAsyncRelayCommand)vm.AnalyzeCommand).ExecuteAsync(null);
        await ((IAsyncRelayCommand)vm.ExportXlsxCommand).ExecuteAsync(null);

        writer.LastXlsxPath.Should().Be(dialogs.SaveFileResult);

        if (dialogs.SaveFileResult is not null && File.Exists(dialogs.SaveFileResult))
        {
            File.Delete(dialogs.SaveFileResult);
        }
    }

    [Fact]
    public async Task Analyze_handles_reader_error_in_row()
    {
        var reader = new FakePdfPageReader(_ => throw new InvalidOperationException("bad pdf"));
        var analyzer = new BatchPdfAnalyzer(reader, new PageAnalysisService(new Iso216FormatRegistry()), 1);
        var vm = new MainViewModel(
            new Iso216FormatRegistry(),
            analyzer,
            new RecordingWriter(),
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
            new RecordingWriter(),
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
            new RecordingWriter(),
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

    [Fact]
    public async Task ExportCsv_not_called_when_save_dialog_canceled()
    {
        var writer = new RecordingWriter();
        var vm = new MainViewModel(
            new Iso216FormatRegistry(),
            new BatchPdfAnalyzer(
                new FakePdfPageReader(_ => new[] { new PageDimensions(1, 595, 842) }),
                new PageAnalysisService(new Iso216FormatRegistry()),
                1),
            writer,
            new RecordingDialogs
            {
                PickFilesResult = new[] { @"C:\demo\a.pdf" },
                SaveFileResult = null,
            },
            Options.Create(new PrintMeterOptions()),
            NullLogger<MainViewModel>.Instance);

        await ((IAsyncRelayCommand)vm.PickFilesCommand).ExecuteAsync(null);
        await ((IAsyncRelayCommand)vm.AnalyzeCommand).ExecuteAsync(null);
        await ((IAsyncRelayCommand)vm.ExportCsvCommand).ExecuteAsync(null);

        writer.CsvCalls.Should().Be(0);
    }

    [Fact]
    public async Task ExportXlsx_not_called_when_save_dialog_canceled()
    {
        var writer = new RecordingWriter();
        var vm = new MainViewModel(
            new Iso216FormatRegistry(),
            new BatchPdfAnalyzer(
                new FakePdfPageReader(_ => new[] { new PageDimensions(1, 595, 842) }),
                new PageAnalysisService(new Iso216FormatRegistry()),
                1),
            writer,
            new RecordingDialogs
            {
                PickFilesResult = new[] { @"C:\demo\a.pdf" },
                SaveFileResult = null,
            },
            Options.Create(new PrintMeterOptions()),
            NullLogger<MainViewModel>.Instance);

        await ((IAsyncRelayCommand)vm.PickFilesCommand).ExecuteAsync(null);
        await ((IAsyncRelayCommand)vm.AnalyzeCommand).ExecuteAsync(null);
        await ((IAsyncRelayCommand)vm.ExportXlsxCommand).ExecuteAsync(null);

        writer.XlsxCalls.Should().Be(0);
    }

    [Fact]
    public async Task ExportCommands_can_execute_only_after_analysis()
    {
        var vm = new MainViewModel(
            new Iso216FormatRegistry(),
            new BatchPdfAnalyzer(
                new FakePdfPageReader(_ => new[] { new PageDimensions(1, 595, 842) }),
                new PageAnalysisService(new Iso216FormatRegistry()),
                1),
            new RecordingWriter(),
            new RecordingDialogs
            {
                PickFilesResult = new[] { @"C:\demo\a.pdf" },
                SaveFileResult = null,
            },
            Options.Create(new PrintMeterOptions()),
            NullLogger<MainViewModel>.Instance);

        vm.ExportCsvCommand.CanExecute(null).Should().BeFalse();
        vm.ExportXlsxCommand.CanExecute(null).Should().BeFalse();

        await ((IAsyncRelayCommand)vm.PickFilesCommand).ExecuteAsync(null);
        await ((IAsyncRelayCommand)vm.AnalyzeCommand).ExecuteAsync(null);

        vm.ExportCsvCommand.CanExecute(null).Should().BeTrue();
        vm.ExportXlsxCommand.CanExecute(null).Should().BeTrue();
    }
}
