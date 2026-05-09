namespace PrintMeter.App.ViewModels;

public interface IFileDialogService
{
    Task<IReadOnlyList<string>?> PickPdfFilesAsync(CancellationToken cancellationToken = default);

    Task<string?> PickFolderAsync(CancellationToken cancellationToken = default);
}
