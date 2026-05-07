using Microsoft.Win32;
using PrintMeter.App.ViewModels;
using WpfApplication = System.Windows.Application;

namespace PrintMeter.App;

public sealed class Win32FileDialogService : IFileDialogService
{
    private sealed class Win32WindowHandle(nint handle) : System.Windows.Forms.IWin32Window
    {
        public nint Handle { get; } = handle;
    }

    public Task<IReadOnlyList<string>?> PickPdfFilesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return WpfApplication.Current.Dispatcher.InvokeAsync(
            () =>
            {
                var dlg = new OpenFileDialog
                {
                    Filter = "PDF (*.pdf)|*.pdf",
                    Multiselect = true,
                };

                var owner = WpfApplication.Current.MainWindow;
                var ok = dlg.ShowDialog(owner) == true;
                return ok ? (IReadOnlyList<string>?)dlg.FileNames : null;
            }).Task;
    }

    public Task<string?> PickFolderAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return WpfApplication.Current.Dispatcher.InvokeAsync(
            () =>
            {
                using var dlg = new System.Windows.Forms.FolderBrowserDialog
                {
                    UseDescriptionForTitle = true,
                    Description = "Выберите папку с PDF",
                };

                var mainWindow = WpfApplication.Current.MainWindow;
                var ownerHandle = mainWindow is null
                    ? nint.Zero
                    : new System.Windows.Interop.WindowInteropHelper(mainWindow).Handle;
                var owner = new Win32WindowHandle(ownerHandle);
                var ok = dlg.ShowDialog(owner) == System.Windows.Forms.DialogResult.OK;
                return ok ? dlg.SelectedPath : null;
            }).Task;
    }

    public Task<string?> SaveFileAsync(string filter, string defaultFileName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return WpfApplication.Current.Dispatcher.InvokeAsync(
            () =>
            {
                var dlg = new SaveFileDialog
                {
                    Filter = filter,
                    FileName = defaultFileName,
                    AddExtension = true,
                };

                var owner = WpfApplication.Current.MainWindow;
                var ok = dlg.ShowDialog(owner) == true;
                return ok ? dlg.FileName : null;
            }).Task;
    }
}
