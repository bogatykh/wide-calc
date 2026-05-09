using Microsoft.UI.Xaml;
using Windows.Storage;
using Windows.Storage.Pickers;
using PrintMeter.App.ViewModels;
using WinRT.Interop;

namespace PrintMeter.App;

public sealed class WinUiFileDialogService : IFileDialogService
{
    public Task<IReadOnlyList<string>?> PickPdfFilesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return RunOnMainAsync(
            async () =>
            {
                var window = RequireWindow();
                var picker = new FileOpenPicker();
                InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(window));
                picker.ViewMode = PickerViewMode.List;
                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                picker.FileTypeFilter.Add(".pdf");
                var files = await picker.PickMultipleFilesAsync();
                if (files.Count == 0)
                {
                    return (IReadOnlyList<string>?)null;
                }

                return (IReadOnlyList<string>?)files.Select(GetPath).ToList();
            });
    }

    public Task<string?> PickFolderAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return RunOnMainAsync(
            async () =>
            {
                var window = RequireWindow();
                var picker = new FolderPicker();
                InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(window));
                picker.FileTypeFilter.Add("*");
                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                var folder = await picker.PickSingleFolderAsync();
                return folder is null ? null : GetPath(folder);
            });
    }

    private static string GetPath(IStorageItem item) =>
        string.IsNullOrEmpty(item.Path) ? item.Name : item.Path;

    private static Window RequireWindow() =>
        App.MainWindowRef ?? throw new InvalidOperationException("Главное окно недоступно для диалога.");

    private static Task<T> RunOnMainAsync<T>(Func<Task<T>> work)
    {
        var window = RequireWindow();
        var dq = window.DispatcherQueue;
        if (dq.HasThreadAccess)
        {
            return work();
        }

        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        dq.TryEnqueue(
            () =>
            {
                StartWork();

                async void StartWork()
                {
                    try
                    {
                        var r = await work().ConfigureAwait(true);
                        tcs.SetResult(r);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                }
            });
        return tcs.Task;
    }
}
