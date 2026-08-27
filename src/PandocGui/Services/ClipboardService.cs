using System.Threading.Tasks;
using Avalonia.Input.Platform;

namespace PandocGui.Services;

public class ClipboardService : IClipboardService
{
    private readonly IClipboard clipboard;

    public ClipboardService(IClipboard clipboard)
    {
        this.clipboard = clipboard;
    }

    public Task SetTextAsync(string text) => clipboard.SetTextAsync(text);
}
