using PrintMeter.Core.Models;

namespace PrintMeter.Core;

public interface IPdfPageReader
{
    /// <summary>Reads page boxes for a PDF. Throws only for unexpected failures; callers may catch.</summary>
    Task<IReadOnlyList<PageDimensions>> ReadPageDimensionsAsync(string filePath, CancellationToken cancellationToken);
}
