using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.UI.Xaml;
using PrintMeter.App.Localization;
using PrintMeter.App.ViewModels;
using PrintMeter.Core;
using PrintMeter.Pdf;
using Serilog;

namespace PrintMeter.App;

public partial class App : Application
{
    private IHost? _host;

    /// <summary>Ссылка для WinRT pickers (HWND главного окна).</summary>
    internal static Window? MainWindowRef { get; private set; }

    public App()
    {
        InitializeComponent();
        UnhandledException += OnUnhandledException;
    }

    private static void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        try
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PrintMeter",
                "logs");
            Directory.CreateDirectory(logDir);
            var path = Path.Combine(logDir, "last-ui-crash.txt");
            var lines = new List<string>
            {
                DateTimeOffset.Now.ToString("O"),
                e.Exception?.ToString() ?? "(null exception)",
            };
            if (e.Exception is COMException cx)
            {
                lines.Add($"HRESULT: 0x{(uint)cx.HResult:X8}");
            }

            File.WriteAllLines(path, lines);
        }
        catch
        {
            // ignore
        }
    }

    public static IServiceProvider Services =>
        ((App)Current)._host?.Services ?? throw new InvalidOperationException("Host not initialized.");

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
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
                        services.AddSingleton<IFileDialogService, WinUiFileDialogService>();
                        services.AddSingleton<IUiStrings, PriUiStrings>();
                        services.AddSingleton<MainViewModel>();
                        services.AddSingleton<MainWindow>();
                    })
                .Build();

            await _host.StartAsync().ConfigureAwait(true);

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            MainWindowRef = mainWindow;
            mainWindow.Closed += MainWindowOnClosed;
            mainWindow.Activate();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application failed to start");
            Log.CloseAndFlush();
            NativeUi.ShowError("PrintMeter", AppText.Format("App_StartError", ex.Message));
            Environment.Exit(1);
        }
    }

    private async void MainWindowOnClosed(object sender, WindowEventArgs args)
    {
        try
        {
            await StopApplicationServicesAsync().ConfigureAwait(true);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    internal async Task StopApplicationServicesAsync()
    {
        if (_host is null)
        {
            return;
        }

        try
        {
            await _host.StopAsync().ConfigureAwait(true);
        }
        finally
        {
            _host.Dispose();
            _host = null;
            MainWindowRef = null;
        }
    }
}
