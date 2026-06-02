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

    public async Task<string> OpenFileAsync(string? filterName = null, IReadOnlyList<string>? extensions = null)
    {
        var options = new FilePickerOpenOptions { AllowMultiple = false };

        if (filterName is not null && extensions is { Count: > 0 })
        {
            options.FileTypeFilter = new[]
            {
                new FilePickerFileType(filterName)
                {
                    Patterns = extensions.Select(extension => "*" + extension).ToList()
                },
                FilePickerFileTypes.All
            };
        }

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
}
