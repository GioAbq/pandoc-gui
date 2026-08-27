using System.Threading.Tasks;

namespace PandocGui.Services;

public interface IClipboardService
{
    Task SetTextAsync(string text);
}
