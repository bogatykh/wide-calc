using Microsoft.UI.Xaml;

namespace PrintMeter.App;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        global::WinRT.ComWrappersSupport.InitializeComWrappers();
        Application.Start(_ => new App());
    }
}
