#nullable enable
namespace PandocGui.CliWrapper;

/// <summary>
/// A reusable snapshot of conversion options (everything except the per-document
/// source and target paths). Persisted as JSON so users can save, reapply,
/// import and export their favourite settings.
/// </summary>
public sealed class Preset
{
    public string Name { get; set; } = "";

    /// <summary>The output writer's file extension (e.g. ".pdf"); pandoc infers the writer from it.</summary>
    public string OutputExtension { get; set; } = PandocFormats.OutputFormats[0].Extension;
    public bool HighlightTheme { get; set; }
    public string HighlightThemeSource { get; set; } = "";
    public bool NumberedHeader { get; set; }
    public bool CustomFont { get; set; }
    public string CustomFontName { get; set; } = "";
    public bool CustomMargin { get; set; }
    public decimal CustomMarginValue { get; set; } = 1.3m;
    public bool CustomPdfEngine { get; set; }
    public string CustomPdfEngineValue { get; set; } = "";
    public bool TableOfContents { get; set; }
}
