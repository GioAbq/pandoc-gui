using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PandocGui.CliWrapper;

public record OutputFormat(string DisplayName, string Extension);

public static class PandocFormats
{
    public const string DefaultInputFormat = "markdown";

    private static readonly IReadOnlyDictionary<string, string> InputFormatByExtension = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".md"] = "markdown",
        [".markdown"] = "markdown",
        [".txt"] = "markdown",
        [".docx"] = "docx",
        [".odt"] = "odt",
        [".html"] = "html",
        [".htm"] = "html",
        [".rst"] = "rst",
        [".tex"] = "latex",
        [".latex"] = "latex",
        [".epub"] = "epub",
        [".rtf"] = "rtf",
        [".org"] = "org",
        [".json"] = "json",
        [".csv"] = "csv",
        [".ipynb"] = "ipynb",
        [".typ"] = "typst",
        [".textile"] = "textile",
        [".wiki"] = "mediawiki",
    };

    public static IReadOnlyList<string> InputFormats { get; } =
        InputFormatByExtension.Values.Distinct().OrderBy(format => format).ToList();

    public static IReadOnlyList<OutputFormat> OutputFormats { get; } = new List<OutputFormat>
    {
        new("PDF", ".pdf"),
        new("Word (docx)", ".docx"),
        new("HTML", ".html"),
        new("EPUB", ".epub"),
        new("Markdown", ".md"),
        new("LaTeX", ".tex"),
        new("OpenDocument (odt)", ".odt"),
        new("RTF", ".rtf"),
        new("PowerPoint (pptx)", ".pptx"),
        new("Plain text", ".txt"),
    };

    public static string DetectInputFormat(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return DefaultInputFormat;
        }

        var extension = Path.GetExtension(path);
        return InputFormatByExtension.TryGetValue(extension, out var format) ? format : DefaultInputFormat;
    }
}
