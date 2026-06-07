using System;
using System.IO;
using PandocGui.CliWrapper;
using Shouldly;
using Xunit;

namespace PandocGui.Tests;

public sealed class PresetServiceTest
{
    private static string NewTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "pandoc-gui-presets-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public void LoadAll_NoFile_ReturnsEmpty()
    {
        var dir = NewTempDir();
        try
        {
            var service = new PresetService(dir);

            service.LoadAll().ShouldBeEmpty();
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void Save_ThenLoadAll_ReturnsPreset()
    {
        var dir = NewTempDir();
        try
        {
            var service = new PresetService(dir);
            service.Save(new Preset
            {
                Name = "A4 PDF",
                OutputExtension = ".pdf",
                TableOfContents = true,
                CustomMargin = true,
                CustomMarginValue = 2.5m
            });

            var loaded = service.LoadAll();

            loaded.Count.ShouldBe(1);
            loaded[0].Name.ShouldBe("A4 PDF");
            loaded[0].TableOfContents.ShouldBeTrue();
            loaded[0].CustomMarginValue.ShouldBe(2.5m);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void Save_SameName_Upserts()
    {
        var dir = NewTempDir();
        try
        {
            var service = new PresetService(dir);
            service.Save(new Preset { Name = "Report", NumberedHeader = false });
            service.Save(new Preset { Name = "report", NumberedHeader = true });

            var loaded = service.LoadAll();

            loaded.Count.ShouldBe(1);
            loaded[0].NumberedHeader.ShouldBeTrue();
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void Delete_RemovesPreset()
    {
        var dir = NewTempDir();
        try
        {
            var service = new PresetService(dir);
            service.Save(new Preset { Name = "Keep" });
            service.Save(new Preset { Name = "Drop" });

            service.Delete("Drop");

            var loaded = service.LoadAll();
            loaded.Count.ShouldBe(1);
            loaded[0].Name.ShouldBe("Keep");
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void Save_BlankName_Throws()
    {
        var dir = NewTempDir();
        try
        {
            var service = new PresetService(dir);

            Should.Throw<ArgumentException>(() => service.Save(new Preset { Name = "  " }));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void Export_ThenImport_RoundTrips()
    {
        var dir = NewTempDir();
        var exportPath = Path.Combine(dir, "exported.json");
        try
        {
            var service = new PresetService(dir);
            var original = new Preset
            {
                Name = "Fancy",
                OutputExtension = ".html",
                CustomFont = true,
                CustomFontName = "Segoe UI",
                CustomPdfEngine = true,
                CustomPdfEngineValue = "xelatex"
            };

            service.Export(original, exportPath);
            var imported = service.Import(exportPath);

            imported.Name.ShouldBe("Fancy");
            imported.OutputExtension.ShouldBe(".html");
            imported.CustomFontName.ShouldBe("Segoe UI");
            imported.CustomPdfEngineValue.ShouldBe("xelatex");
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void Import_InvalidFile_Throws()
    {
        var dir = NewTempDir();
        var badPath = Path.Combine(dir, "bad.json");
        File.WriteAllText(badPath, "{ not a preset");
        try
        {
            var service = new PresetService(dir);

            Should.Throw<Exception>(() => service.Import(badPath));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}
