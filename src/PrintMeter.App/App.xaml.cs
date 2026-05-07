using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PrintMeter.App.ViewModels;
using PrintMeter.Core;
using PrintMeter.Export;
using PrintMeter.Pdf;
using Serilog;

namespace PrintMeter.App;

public partial class App : Application
{
    private IHost? _host;

    public static IServiceProvider Services =>
        ((App)Current)._host?.Services ?? throw new InvalidOperationException("Host not initialized.");

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PrintMeter",
            "logs");
        Directory.CreateDirectory(logDir);
        var logPath = Path.Combine(logDir, "printmeter-.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14)
            .CreateLogger();

        try
        {
            _host = Host.CreateDefaultBuilder()
                .UseSerilog()
                .ConfigureAppConfiguration(
                    (_, cfg) =>
                    {
                        cfg.SetBasePath(AppContext.BaseDirectory);
                        cfg.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                    })
                .ConfigureServices(
                    (ctx, services) =>
                    {
                        services.Configure<PrintMeterOptions>(ctx.Configuration.GetSection(PrintMeterOptions.SectionName));

                        services.AddSingleton<IFormatRegistry>(_ => new Iso216FormatRegistry());
                        services.AddSingleton<PageAnalysisService>();
                        services.AddSingleton<IPdfPageReader, PdfPigPageReader>();
                        services.AddSingleton(
                            sp =>
                            {
                                var opt = sp.GetRequiredService<IOptions<PrintMeterOptions>>().Value;
                                return new BatchPdfAnalyzer(
                                    sp.GetRequiredService<IPdfPageReader>(),
                                    sp.GetRequiredService<PageAnalysisService>(),
                                    opt.MaxDegreeOfParallelism);
                            });
                        services.AddSingleton<CsvBatchReportExporter>();
                        services.AddSingleton<XlsxBatchReportExporter>();
                        services.AddSingleton<IBatchReportWriter, BatchReportWriter>();
                        services.AddSingleton<IFileDialogService, Win32FileDialogService>();
                        services.AddSingleton<MainViewModel>();
                        services.AddSingleton<MainWindow>();
                    })
                .Build();

            await _host.StartAsync().ConfigureAwait(true);

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            MainWindow = mainWindow;
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application failed to start");
            MessageBox.Show(ex.ToString(), "PrintMeter — ошибка запуска", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            if (_host is not null)
            {
                await _host.StopAsync().ConfigureAwait(true);
                _host.Dispose();
            }
        }
        finally
        {
            Log.CloseAndFlush();
        }

        base.OnExit(e);
    }
}
