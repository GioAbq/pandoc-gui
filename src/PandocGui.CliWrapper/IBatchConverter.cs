#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PandocGui.CliWrapper;

public sealed record BatchItemResult(string SourcePath, string TargetPath, bool Success, string? Error);

public interface IBatchConverter
{
    /// <summary>
    /// Converts each source file to <paramref name="outputExtension"/> next to the source,
    /// reusing the options from <paramref name="optionsTemplate"/>. A failure on one file does
    /// not stop the rest; every file gets a result. Progress is reported as each file completes.
    /// </summary>
    Task<IReadOnlyList<BatchItemResult>> ConvertAsync(
        IReadOnlyList<string> sources,
        string outputExtension,
        PandocParameters optionsTemplate,
        IProgress<BatchItemResult>? progress = null,
        CancellationToken cancellationToken = default);
}
