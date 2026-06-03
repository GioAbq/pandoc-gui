#nullable enable
using System;

namespace PandocGui.CliWrapper;

public enum PandocAvailability
{
    Unknown,
    Installed,
    NotInstalled
}

/// <summary>
/// Snapshot of the pandoc environment as seen at startup (and after an install/upgrade).
/// </summary>
public record PandocStatus(
    PandocAvailability Availability,
    Version? InstalledVersion,
    Version? LatestVersion,
    bool UpdateAvailable,
    bool CanInstallViaWinget,
    string? ManualInstructions);
