#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace PandocGui.CliWrapper;

/// <summary>
/// Stores all presets in a single JSON file under the application data directory.
/// </summary>
public sealed class PresetService : IPresetService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string filePath;

    public PresetService(string dataDirectoryPath)
    {
        filePath = Path.Combine(dataDirectoryPath, "presets.json");
    }

    public IReadOnlyList<Preset> LoadAll()
    {
        if (!File.Exists(filePath))
        {
            return Array.Empty<Preset>();
        }

        try
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<Preset>>(json, JsonOptions) ?? new List<Preset>();
        }
        catch (JsonException)
        {
            // A corrupt file should not crash the app; treat it as no presets.
            return Array.Empty<Preset>();
        }
    }

    public void Save(Preset preset)
    {
        ArgumentNullException.ThrowIfNull(preset);
        if (string.IsNullOrWhiteSpace(preset.Name))
        {
            throw new ArgumentException("Preset name is required.", nameof(preset));
        }

        var all = LoadAll()
            .Where(existing => !NameEquals(existing.Name, preset.Name))
            .ToList();
        all.Add(preset);
        Write(all);
    }

    public void Delete(string name)
    {
        var all = LoadAll()
            .Where(existing => !NameEquals(existing.Name, name))
            .ToList();
        Write(all);
    }

    public Preset Import(string path)
    {
        var json = File.ReadAllText(path);
        var preset = JsonSerializer.Deserialize<Preset>(json, JsonOptions);
        if (preset is null || string.IsNullOrWhiteSpace(preset.Name))
        {
            throw new InvalidDataException("The selected file is not a valid preset.");
        }

        return preset;
    }

    public void Export(Preset preset, string path)
    {
        ArgumentNullException.ThrowIfNull(preset);
        File.WriteAllText(path, JsonSerializer.Serialize(preset, JsonOptions));
    }

    private void Write(IReadOnlyList<Preset> presets)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(filePath, JsonSerializer.Serialize(presets, JsonOptions));
    }

    private static bool NameEquals(string a, string b) =>
        string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
