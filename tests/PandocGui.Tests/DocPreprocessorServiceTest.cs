using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using PandocGui.CliWrapper;
using Shouldly;
using Xunit;

namespace PandocGui.Tests;

public sealed class DocPreprocessorServiceTest
{
    [Fact]
    public async Task ConvertToDocxAsync_MissingFile_ThrowsFileNotFound()
    {
        var service = new DocPreprocessorService();

        await Should.ThrowAsync<FileNotFoundException>(
            () => service.ConvertToDocxAsync("does-not-exist.doc"));
    }

    [Fact]
    public void ConvertWithDocSharp_ProducesReadableDocx()
    {
        var docPath = Path.Combine(AppContext.BaseDirectory, "test-files", "sample.doc");
        File.Exists(docPath).ShouldBeTrue($"fixture missing at {docPath}");

        var docxPath = Path.Combine(Path.GetTempPath(), $"pandoc-gui-test-{Guid.NewGuid():N}.docx");
        try
        {
            DocPreprocessorService.ConvertWithDocSharp(docPath, docxPath);

            File.Exists(docxPath).ShouldBeTrue();
            new FileInfo(docxPath).Length.ShouldBeGreaterThan(0);

            using var zip = ZipFile.OpenRead(docxPath);
            zip.Entries.ShouldContain(e => e.FullName == "word/document.xml");
        }
        finally
        {
            if (File.Exists(docxPath))
            {
                File.Delete(docxPath);
            }
        }
    }
}
