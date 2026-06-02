using System.Collections.Generic;
using System.Threading.Tasks;

namespace PandocGui.Services;

public interface IFileDialogService
{
    Task<string> OpenFileAsync(string? filterName = null, IReadOnlyList<string>? extensions = null);
    Task<string> SaveFileAsync();
}
