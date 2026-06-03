#nullable enable
using System;
using System.Text.RegularExpressions;

namespace PandocGui.CliWrapper;

public enum OsKind
{
    Windows,
    MacOs,
    Linux,
    Other
}

/// <summary>
/// Pure, UI-free, testable logic around the pandoc environment: version parsing,
/// version comparison and per-OS install guidance. All I/O lives in
/// <see cref="PandocEnvironmentService"/>.
/// </summary>
public static class PandocEnvironment
{
    public const string WingetPackageId = "JohnMacFarlane.Pandoc";
    public const string DownloadUrl = "https://pandoc.org/installing.html";

    private static readonly Regex PandocVersionRegex =
        new(@"pandoc(?:\.exe)?\s+(\d+(?:\.\d+){1,3})", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // winget output is localized; cover the common English/Polish labels, fail soft otherwise.
    private static readonly Regex WingetShowVersionRegex =
        new(@"(?:Version|Wersja)\s*:\s*v?(\d+(?:\.\d+){1,3})", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static OsKind GetCurrentOs()
    {
        if (OperatingSystem.IsWindows()) return OsKind.Windows;
        if (OperatingSystem.IsMacOS()) return OsKind.MacOs;
        if (OperatingSystem.IsLinux()) return OsKind.Linux;
        return OsKind.Other;
    }

    /// <summary>Parses the version from <c>pandoc --version</c> output, e.g. "pandoc 3.9.0.2".</summary>
    public static Version? ParseVersion(string? output)
    {
        if (string.IsNullOrWhiteSpace(output)) return null;
        var match = PandocVersionRegex.Match(output);
        return match.Success && Version.TryParse(match.Groups[1].Value, out var version) ? version : null;
    }

    /// <summary>Parses the available version from <c>winget show</c> output (best-effort, localized).</summary>
    public static Version? ParseWingetShowVersion(string? output)
    {
        if (string.IsNullOrWhiteSpace(output)) return null;
        var match = WingetShowVersionRegex.Match(output);
        return match.Success && Version.TryParse(match.Groups[1].Value, out var version) ? version : null;
    }

    public static bool IsUpdateAvailable(Version? current, Version? latest) =>
        current is not null && latest is not null && latest > current;

    public static string GetManualInstallInstructions(OsKind os) => os switch
    {
        OsKind.MacOs =>
            "Pandoc not found. Install it with Homebrew:\n    brew install pandoc\n" +
            $"or download it from {DownloadUrl}",
        OsKind.Linux =>
            "Pandoc not found. Install it with your package manager, e.g.\n" +
            "    sudo apt install pandoc   (Debian/Ubuntu)\n" +
            "    sudo dnf install pandoc   (Fedora)\n" +
            $"or download it from {DownloadUrl}",
        _ =>
            $"Pandoc not found. Download and install it from {DownloadUrl}"
    };
}
