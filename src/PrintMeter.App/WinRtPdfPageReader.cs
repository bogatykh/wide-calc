using PrintMeter.Core;
using PrintMeter.Core.Models;
using Windows.Data.Pdf;
using Windows.Storage;

namespace PrintMeter.App;

/// <summary>
/// Uses Windows.Data.Pdf to avoid external PDF dependencies in WinUI app compile graph.
/// </summary>
public sealed class WinRtPdfPageReader : IPdfPageReader
{
    public async Task<IReadOnlyList<PageDimensions>> ReadPageDimensionsAsync(string filePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var storageFile = await StorageFile.GetFileFromPathAsync(filePath);
        var document = await PdfDocument.LoadFromFileAsync(storageFile);
        var pages = new List<PageDimensions>((int)document.PageCount);

        for (uint i = 0; i < document.PageCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var page = document.GetPage(i);
            var size = page.Size;
            pages.Add(new PageDimensions((int)i + 1, size.Width, size.Height));
        }

        return pages;
    }
}
