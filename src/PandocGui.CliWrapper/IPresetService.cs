#nullable enable
using System.Collections.Generic;

namespace PandocGui.CliWrapper;

public interface IPresetService
{
    IReadOnlyList<Preset> LoadAll();
    void Save(Preset preset);
    void Delete(string name);
    Preset Import(string path);
    void Export(Preset preset, string path);
}
