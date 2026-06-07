using PandocGui.CliWrapper;
using Shouldly;
using Xunit;

namespace PandocGui.Tests;

public sealed class PandocErrorMessagesTest
{
    [Fact]
    public void Describe_PdfProgramNotFound_GuidesToInstallEngine()
    {
        var message = PandocErrorMessages.Describe(PandocErrorCode.PandocPDFProgramNotFoundError);

        message.ShouldContain("PDF engine");
        message.ShouldContain("LaTeX");
        message.ShouldNotContain("PandocPDFProgramNotFoundError");
    }

    [Theory]
    [InlineData(PandocErrorCode.PandocPDFError)]
    [InlineData(PandocErrorCode.PandocMakePDFError)]
    public void Describe_PdfFailures_MentionPdfEngine(PandocErrorCode error)
    {
        PandocErrorMessages.Describe(error).ShouldContain("PDF engine");
    }

    [Fact]
    public void Describe_UnknownReader_MentionsInputFormat()
    {
        PandocErrorMessages.Describe(PandocErrorCode.PandocUnknownReaderError)
            .ShouldContain("input format");
    }

    [Fact]
    public void Describe_UnknownWriter_MentionsOutputFormat()
    {
        PandocErrorMessages.Describe(PandocErrorCode.PandocUnknownWriterError)
            .ShouldContain("output format");
    }

    [Fact]
    public void Describe_UnmappedCode_FallsBackToCodeAndName()
    {
        var message = PandocErrorMessages.Describe(PandocErrorCode.PandocShouldNeverHappenError);

        message.ShouldContain("62");
        message.ShouldContain("PandocShouldNeverHappenError");
    }
}
