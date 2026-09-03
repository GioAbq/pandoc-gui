using PandocGui.CliWrapper;
using Xunit;

namespace PandocGui.Tests;

public class PandocFormatsTest
{
    [Fact]
    public void OutputFormat_RendersAsItsDisplayName()
    {
        // Given
        var format = new OutputFormat("PDF", "pdf");

        // When
        var rendered = format.ToString();

        // Then
        // The output format combo box binds the records directly, with no item template,
        // so the ComboBox shows whatever ToString returns.
        Assert.Equal("PDF", rendered);
    }
}
