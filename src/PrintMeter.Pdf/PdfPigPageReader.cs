using PrintMeter.Core;
using PrintMeter.Core.Models;
using UglyToad.PdfPig;

namespace PrintMeter.Pdf;

public sealed class PdfPigPageReader : IPdfPageReader
{
    public Task<IReadOnlyList<PageDimensions>> ReadPageDimensionsAsync(string filePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var document = PdfDocument.Open(filePath);
        var list = new List<PageDimensions>();

        foreach (var page in document.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var w = page.Width;
            var h = page.Height;
            list.Add(new PageDimensions(page.Number, w, h));
        }

        return Task.FromResult<IReadOnlyList<PageDimensions>>(list);
    }
}
