using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace PrintMeter.App;

internal static class Program
{
    [STAThread]
    private static void Main(string[] _)
    {
        global::WinRT.ComWrappersSupport.InitializeComWrappers();

        Application.Start(static _ =>
        {
            var dq = DispatcherQueue.GetForCurrentThread();
            var sync = new DispatcherQueueSynchronizationContext(dq);
            System.Threading.SynchronizationContext.SetSynchronizationContext(sync);
            _ = new App();
        });
    }
}
