using FluentAssertions;
using PrintMeter.Core;
using Xunit;

namespace PrintMeter.Core.Tests;

public sealed class PdfFileDiscoveryTests
{
    [Fact]
    public void EnumeratePdfFilesInFolder_respects_recursive_flag()
    {
        var root = Path.Combine(Path.GetTempPath(), $"pm-discovery-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var nested = Path.Combine(root, "nested");
        Directory.CreateDirectory(nested);

        var topPdf = Path.Combine(root, "top.pdf");
        var nestedPdf = Path.Combine(nested, "nested.pdf");
        File.WriteAllText(topPdf, "x");
        File.WriteAllText(nestedPdf, "x");

        try
        {
            var topOnly = PdfFileDiscovery.EnumeratePdfFilesInFolder(root, recursive: false);
            var recursive = PdfFileDiscovery.EnumeratePdfFilesInFolder(root, recursive: true);

            topOnly.Should().ContainSingle().Which.Should().Be(topPdf);
            recursive.Should().HaveCount(2);
            recursive.Should().Contain(topPdf);
            recursive.Should().Contain(nestedPdf);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
