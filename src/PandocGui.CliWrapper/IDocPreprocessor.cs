using System.Threading;
using System.Threading.Tasks;

namespace PandocGui.CliWrapper;

/// <summary>
/// Converts legacy Word 97-2003 binary <c>.doc</c> files into <c>.docx</c>, which pandoc can read.
/// </summary>
public interface IDocPreprocessor
{
    /// <summary>
    /// Converts the given <c>.doc</c> file to a temporary <c>.docx</c> and returns its path.
    /// Throws <see cref="System.InvalidOperationException"/> when no converter is available.
    /// </summary>
    Task<string> ConvertToDocxAsync(string docPath, CancellationToken cancellationToken = default);
}
