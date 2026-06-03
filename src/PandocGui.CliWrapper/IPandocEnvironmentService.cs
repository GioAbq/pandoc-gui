#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PandocGui.CliWrapper;

public interface IPandocEnvironmentService
{
    /// <summary>Detects whether pandoc is on PATH and parses its version. Never throws on a missing executable.</summary>
    Task<PandocStatus> DetectAsync(CancellationToken cancellationToken = default);

    /// <summary>Latest version offered by winget (Windows + winget only); null elsewhere or on any failure.</summary>
    Task<Version?> GetLatestWingetVersionAsync(CancellationToken cancellationToken = default);

    /// <summary>Installs pandoc via winget (Windows only). Returns true on success.</summary>
    Task<bool> InstallAsync(CancellationToken cancellationToken = default);

    /// <summary>Upgrades pandoc via winget (Windows only). Returns true on success.</summary>
    Task<bool> UpgradeAsync(CancellationToken cancellationToken = default);
}
