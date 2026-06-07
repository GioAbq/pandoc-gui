using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace PandocGui.Services;

public class FileDialogService : IFileDialogService
{
    private readonly Window window;

    public FileDialogService(Window window)
    {
        this.window = window;
    }

    public async Task<string> OpenFileAsync(IReadOnlyList<FilePickerGroup>? groups = null)
    {
        var options = new FilePickerOpenOptions { AllowMultiple = false, FileTypeFilter = BuildFilters(groups) };

        try
        {
            var files = await window.StorageProvider.OpenFilePickerAsync(options);
            return files.Count == 0 ? "" : files[0].TryGetLocalPath() ?? "";
        }
        catch (Exception)
        {
            return "";
        }
    }

    public async Task<IReadOnlyList<string>> OpenFilesAsync(IReadOnlyList<FilePickerGroup>? groups = null)
    {
        var options = new FilePickerOpenOptions { AllowMultiple = true, FileTypeFilter = BuildFilters(groups) };

        try
        {
            var files = await window.StorageProvider.OpenFilePickerAsync(options);
            return files
                .Select(file => file.TryGetLocalPath())
                .Where(path => !string.IsNullOrEmpty(path))
                .Cast<string>()
                .ToList();
        }
        catch (Exception)
        {
            return Array.Empty<string>();
        }
    }

    public async Task<string> OpenFolderAsync()
    {
        try
        {
            var folders = await window.StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions { AllowMultiple = false });
            return folders.Count == 0 ? "" : folders[0].TryGetLocalPath() ?? "";
        }
        catch (Exception)
        {
            return "";
        }
    }

    public async Task<string> SaveFileAsync()
    {
        var dialog = new FilePickerSaveOptions();
        try
        {
            var file = await window.StorageProvider.SaveFilePickerAsync(dialog);
            return file?.TryGetLocalPath() ?? "";
        }
        catch (Exception)
        {
            return "";
        }
    }

    private static List<FilePickerFileType>? BuildFilters(IReadOnlyList<FilePickerGroup>? groups)
    {
        if (groups is not { Count: > 0 })
        {
            return null;
        }

        var allExtensions = groups.SelectMany(group => group.Extensions).Distinct().ToList();

        var filters = new List<FilePickerFileType>
        {
            new("All supported documents") { Patterns = ToPatterns(allExtensions) }
        };
        filters.AddRange(groups.Select(group =>
            new FilePickerFileType(group.Name) { Patterns = ToPatterns(group.Extensions) }));
        filters.Add(FilePickerFileTypes.All);

        return filters;
    }

    private static List<string> ToPatterns(IEnumerable<string> extensions) =>
        extensions.Select(extension => "*" + extension).ToList();
}
