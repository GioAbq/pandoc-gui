#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PandocGui.CliWrapper;

public sealed class BatchConverter : IBatchConverter
{
    private readonly IPandocCli pandoc;

    public BatchConverter(IPandocCli pandoc)
    {
        this.pandoc = pandoc;
    }

    public async Task<IReadOnlyList<BatchItemResult>> ConvertAsync(
        IReadOnlyList<string> sources,
        string outputExtension,
        PandocParameters optionsTemplate,
        IProgress<BatchItemResult>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(optionsTemplate);

        var results = new List<BatchItemResult>(sources.Count);

        foreach (var source in sources)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var target = BuildTargetPath(source, outputExtension);
            BatchItemResult result;
            try
            {
                await pandoc.ExportPdfAsync(CloneWith(optionsTemplate, source, target));
                result = new BatchItemResult(source, target, true, null);
            }
            catch (Exception ex)
            {
                result = new BatchItemResult(source, target, false, ex.Message);
            }

            results.Add(result);
            progress?.Report(result);
        }

        return results;
    }

    private static string BuildTargetPath(string source, string outputExtension)
    {
        var directory = Path.GetDirectoryName(source) ?? "";
        var name = Path.GetFileNameWithoutExtension(source);
        return Path.Combine(directory, name + outputExtension);
    }

    private static PandocParameters CloneWith(PandocParameters template, string source, string target) => new()
    {
        SourcePath = source,
        TargetPath = target,
        SourceFormat = PandocFormats.DetectInputFormat(source),
        HighlightTheme = template.HighlightTheme,
        HighlightThemeSource = template.HighlightThemeSource,
        NumberedHeader = template.NumberedHeader,
        CustomFont = template.CustomFont,
        CustomFontName = template.CustomFontName,
        CustomMargin = template.CustomMargin,
        CustomMarginValue = template.CustomMarginValue,
        CustomPdfEngine = template.CustomPdfEngine,
        CustomPdfEngineValue = template.CustomPdfEngineValue,
        TableOfContents = template.TableOfContents,
        LogToFile = template.LogToFile,
        LogFilePath = template.LogFilePath
    };
}
