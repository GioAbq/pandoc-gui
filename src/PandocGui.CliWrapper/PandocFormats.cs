using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PandocGui.CliWrapper;

public record InputFormat(string DisplayName, string Format, IReadOnlyList<string> Extensions)
{
    public override string ToString() => DisplayName;
}

public record OutputFormat(string DisplayName, string Extension)
{
    public override string ToString() => DisplayName;
}

public static class PandocFormats
{
    public const string DefaultInputFormat = "markdown";

    // Ordered by real-world popularity, most common first.
    public static IReadOnlyList<InputFormat> InputFormats { get; } = new List<InputFormat>
    {
        new("Markdown", "markdown", new[] { ".md", ".markdown", ".txt" }),
        new("Word (docx)", "docx", new[] { ".docx" }),
        new("HTML", "html", new[] { ".html", ".htm" }),
        new("LaTeX", "latex", new[] { ".tex", ".latex" }),
        new("reStructuredText", "rst", new[] { ".rst" }),
        new("EPUB", "epub", new[] { ".epub" }),
        new("OpenDocument (odt)", "odt", new[] { ".odt" }),
        new("Rich Text (rtf)", "rtf", new[] { ".rtf" }),
        new("Org mode", "org", new[] { ".org" }),
        new("Jupyter Notebook", "ipynb", new[] { ".ipynb" }),
        new("Typst", "typst", new[] { ".typ" }),
        new("MediaWiki", "mediawiki", new[] { ".wiki" }),
        new("Textile", "textile", new[] { ".textile" }),
        new("CSV", "csv", new[] { ".csv" }),
        new("JSON", "json", new[] { ".json" }),
    };

    // Ordered by real-world popularity, most common first.
    public static IReadOnlyList<OutputFormat> OutputFormats { get; } = new List<OutputFormat>
    {
        new("PDF", ".pdf"),
        new("Word (docx)", ".docx"),
        new("HTML", ".html"),
        new("Markdown", ".md"),
        new("EPUB", ".epub"),
        new("OpenDocument (odt)", ".odt"),
        new("Rich Text (rtf)", ".rtf"),
        new("LaTeX", ".tex"),
        new("PowerPoint (pptx)", ".pptx"),
        new("Plain text", ".txt"),
    };

    public static IReadOnlyList<string> InputExtensions { get; } =
        InputFormats.SelectMany(format => format.Extensions).Distinct().ToList();

    public static string DetectInputFormat(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return DefaultInputFormat;
        }

        var extension = Path.GetExtension(path);
        var match = InputFormats.FirstOrDefault(
            format => format.Extensions.Contains(extension, StringComparer.OrdinalIgnoreCase));
        return match?.Format ?? DefaultInputFormat;
    }

    public static bool IsSupportedInput(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var extension = Path.GetExtension(path);
        return InputExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }
}
