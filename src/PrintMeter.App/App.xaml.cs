using Microsoft.Extensions.DependencyInjection;

namespace PrintMeter.App;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var services = MauiProgram.BuiltApp?.Services
            ?? throw new InvalidOperationException("Maui application host is not initialized.");

        var page = services.GetRequiredService<MainPage>();
        return new Window(page) { Title = "PrintMeter — длина печати по PDF" };
    }
}
