using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrintMeter.App.ViewModels;
using PrintMeter.Core;
using PrintMeter.Pdf;
using Serilog;

namespace PrintMeter.App;

public static class MauiProgram
{
    internal static MauiApp? BuiltApp { get; private set; }

    public static MauiApp CreateMauiApp()
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

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(_ => { });

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(dispose: true);

        builder.Configuration
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        builder.Services.Configure<PrintMeterOptions>(
            builder.Configuration.GetSection(PrintMeterOptions.SectionName));

        builder.Services.AddSingleton<IFormatRegistry>(_ => new Iso216FormatRegistry());
        builder.Services.AddSingleton<PageAnalysisService>();
        builder.Services.AddSingleton<IPdfPageReader, PdfPigPageReader>();
        builder.Services.AddSingleton(
            sp =>
            {
                var opt = sp.GetRequiredService<IOptions<PrintMeterOptions>>().Value;
                return new BatchPdfAnalyzer(
                    sp.GetRequiredService<IPdfPageReader>(),
                    sp.GetRequiredService<PageAnalysisService>(),
                    opt.MaxDegreeOfParallelism);
            });
        builder.Services.AddSingleton<IFileDialogService, MauiFileDialogService>();
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();
        BuiltApp = app;
        return app;
    }
}
