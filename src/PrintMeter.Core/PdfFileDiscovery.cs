namespace PrintMeter.Core;

public static class PdfFileDiscovery
{
    public static IReadOnlyList<string> EnumeratePdfFilesInFolder(string folderPath, bool recursive)
    {
        var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        return Directory
            .EnumerateFiles(folderPath, "*.pdf", option)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
