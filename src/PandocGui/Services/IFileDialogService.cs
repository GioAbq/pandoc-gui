using System.Collections.Generic;
using System.Threading.Tasks;

namespace PandocGui.Services;

public record FilePickerGroup(string Name, IReadOnlyList<string> Extensions);

public interface IFileDialogService
{
    Task<string> OpenFileAsync(IReadOnlyList<FilePickerGroup>? groups = null);
    Task<IReadOnlyList<string>> OpenFilesAsync(IReadOnlyList<FilePickerGroup>? groups = null);
    Task<string> OpenFolderAsync();
    Task<string> SaveFileAsync();
}
