#if !PRINTMETER_WINDOWS_APPSDK_SELF_CONTAINED
using System.IO;
using Microsoft.Windows.ApplicationModel.DynamicDependency;
#endif
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace PrintMeter.App;

public static class Program
{
#if !PRINTMETER_WINDOWS_APPSDK_SELF_CONTAINED
    private const uint SdkMajorMinor = 0x00010006;

    private static readonly Windows.ApplicationModel.PackageVersion s_minRuntimeVersion = new(0x1770020701490000UL);
#endif

    [STAThread]
    private static void Main(string[] _)
    {
#if !PRINTMETER_WINDOWS_APPSDK_SELF_CONTAINED
        if (
            !Bootstrap.TryInitialize(
                SdkMajorMinor,
                string.Empty,
                s_minRuntimeVersion,
                Bootstrap.InitializeOptions.OnNoMatch_ShowUI,
                out var hr))
        {
            var note =
                $"Не удалось инициализировать Windows App SDK 1.6 (HRESULT 0x{hr:X8}).\n\n"
                + "Установите «Windows App Runtime» (x64) с релизов Windows App SDK или используйте сборку self-contained.";
            NativeUi.ShowError("PrintMeter", note);
            TryWriteBootstrapFailureLog(note, hr);
            Environment.Exit(hr);
        }
#endif

        global::WinRT.ComWrappersSupport.InitializeComWrappers();

        Application.Start(static _ =>
        {
            var dq = DispatcherQueue.GetForCurrentThread();
            var sync = new DispatcherQueueSynchronizationContext(dq);
            System.Threading.SynchronizationContext.SetSynchronizationContext(sync);
            _ = new App();
        });
    }

#if !PRINTMETER_WINDOWS_APPSDK_SELF_CONTAINED
    private static void TryWriteBootstrapFailureLog(string message, int hr)
    {
        try
        {
            var path = Path.Combine(Path.GetTempPath(), "PrintMeter-bootstrap-failure.txt");
            File.WriteAllText(
                path,
                $"{DateTimeOffset.Now:O}\nHRESULT: 0x{hr:X8}\n\n{message}\n");
        }
        catch
        {
            // Игнорируем: диалог уже показан.
        }
    }
#endif
}
