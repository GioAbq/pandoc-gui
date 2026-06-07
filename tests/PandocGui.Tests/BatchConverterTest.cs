using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NSubstitute;
using PandocGui.CliWrapper;
using Shouldly;
using Xunit;

namespace PandocGui.Tests;

public sealed class BatchConverterTest
{
    private sealed class SyncProgress<T> : IProgress<T>
    {
        public List<T> Reports { get; } = new();
        public void Report(T value) => Reports.Add(value);
    }

    [Fact]
    public async Task ConvertAsync_AllSucceed_ReturnsSuccessForEach()
    {
        var pandoc = Substitute.For<IPandocCli>();
        var converter = new BatchConverter(pandoc);
        var sources = new[] { "/a/one.md", "/a/two.md" };

        var results = await converter.ConvertAsync(sources, ".pdf", new PandocParameters());

        results.Count.ShouldBe(2);
        results.ShouldAllBe(result => result.Success);
        await pandoc.Received(2).ExportPdfAsync(Arg.Any<PandocParameters>());
    }

    [Fact]
    public async Task ConvertAsync_OneFails_OthersStillSucceed()
    {
        var pandoc = Substitute.For<IPandocCli>();
        pandoc.ExportPdfAsync(Arg.Is<PandocParameters>(parameters => parameters.SourcePath.EndsWith("bad.md")))
            .Returns(Task.FromException(new InvalidOperationException("boom")));
        var converter = new BatchConverter(pandoc);
        var sources = new[] { "/a/good.md", "/a/bad.md", "/a/good2.md" };

        var results = await converter.ConvertAsync(sources, ".pdf", new PandocParameters());

        results.Count.ShouldBe(3);
        results.Count(result => result.Success).ShouldBe(2);
        var failed = results.Single(result => !result.Success);
        failed.SourcePath.ShouldBe("/a/bad.md");
        failed.Error.ShouldBe("boom");
    }

    [Fact]
    public async Task ConvertAsync_DerivesTargetPath_WithOutputExtension()
    {
        var pandoc = Substitute.For<IPandocCli>();
        var converter = new BatchConverter(pandoc);
        var source = Path.Combine("docs", "report.md");

        var results = await converter.ConvertAsync(new[] { source }, ".html", new PandocParameters());

        results[0].TargetPath.ShouldBe(Path.Combine("docs", "report.html"));
    }

    [Fact]
    public async Task ConvertAsync_PassesPerFileSourceAndDetectedFormat()
    {
        var pandoc = Substitute.For<IPandocCli>();
        var converter = new BatchConverter(pandoc);
        var template = new PandocParameters { TableOfContents = true };

        await converter.ConvertAsync(new[] { "paper.docx" }, ".pdf", template);

        await pandoc.Received(1).ExportPdfAsync(Arg.Is<PandocParameters>(parameters =>
            parameters.SourcePath == "paper.docx"
            && parameters.SourceFormat == "docx"
            && parameters.TableOfContents));
    }

    [Fact]
    public async Task ConvertAsync_ReportsProgress_ForEachFile()
    {
        var pandoc = Substitute.For<IPandocCli>();
        var converter = new BatchConverter(pandoc);
        var progress = new SyncProgress<BatchItemResult>();
        var sources = new[] { "/a/one.md", "/a/two.md", "/a/three.md" };

        await converter.ConvertAsync(sources, ".pdf", new PandocParameters(), progress);

        progress.Reports.Count.ShouldBe(3);
        progress.Reports.ShouldAllBe(result => result.Success);
    }
}
