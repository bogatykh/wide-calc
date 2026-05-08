using System.Runtime.InteropServices;

namespace PrintMeter.App;

internal static partial class NativeUi
{
    private const uint MbOk = 0;
    private const uint MbIconError = 0x00000010;

    [LibraryImport("user32.dll", EntryPoint = "MessageBoxW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int MessageBox(nint hWnd, string text, string caption, uint type);

    internal static void ShowError(string caption, string text) =>
        MessageBox(0, text, caption, MbOk | MbIconError);
}
