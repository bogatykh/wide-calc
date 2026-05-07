using CommunityToolkit.Maui.Storage;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using PrintMeter.App.ViewModels;

namespace PrintMeter.App;

public sealed class MauiFileDialogService : IFileDialogService
{
    public async Task<IReadOnlyList<string>?> PickPdfFilesAsync(CancellationToken cancellationToken = default)
    {
        return await MainThread.InvokeOnMainThreadAsync(
            async () =>
            {
                var result = await FilePicker.Default.PickMultipleAsync(
                    new PickOptions
                    {
                        PickerTitle = "Выберите PDF",
                        FileTypes = FilePickerFileType.Pdf,
                    });

                if (result is null)
                {
                    return (IReadOnlyList<string>?)null;
                }

                var paths = result
                    .Where(f => f is not null)
                    .Select(f => f!.FullPath)
                    .Where(static p => !string.IsNullOrWhiteSpace(p))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return paths.Count == 0 ? null : paths;
            }).ConfigureAwait(false);
    }

    public async Task<string?> PickFolderAsync(CancellationToken cancellationToken = default)
    {
        return await MainThread.InvokeOnMainThreadAsync(
            async () =>
            {
                var result = await FolderPicker.Default.PickAsync(cancellationToken).ConfigureAwait(true);
                if (!result.IsSuccessful || result.Folder is null || string.IsNullOrWhiteSpace(result.Folder.Path))
                {
                    return null;
                }

                return result.Folder.Path;
            }).ConfigureAwait(false);
    }
}
