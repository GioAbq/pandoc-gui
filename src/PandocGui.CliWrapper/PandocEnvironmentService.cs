#nullable enable
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace PandocGui.CliWrapper;

public class PandocEnvironmentService : IPandocEnvironmentService
{
    private const int ProbeTimeoutMs = 15_000;

    // winget presence is stable for the lifetime of the process; probe once.
    private bool? wingetAvailable;

    public async Task<PandocStatus> DetectAsync(CancellationToken cancellationToken = default)
    {
        var os = PandocEnvironment.GetCurrentOs();
        var canInstall = await CanInstallViaWingetAsync(cancellationToken);
        var manualInstructions = canInstall ? null : PandocEnvironment.GetManualInstallInstructions(os);

        var result = await RunProcessAsync("pandoc", "--version", ProbeTimeoutMs, cancellationToken);
        if (!result.Started || result.ExitCode != 0)
        {
            Log.Information("Pandoc not detected (started: {Started}, exit code: {ExitCode})",
                result.Started, result.ExitCode);
            return new PandocStatus(PandocAvailability.NotInstalled, null, null, false, canInstall, manualInstructions);
        }

        var version = PandocEnvironment.ParseVersion(result.StandardOutput);
        Log.Information("Pandoc detected, version {Version}", version);
        return new PandocStatus(PandocAvailability.Installed, version, null, false, canInstall, manualInstructions);
    }

    public async Task<Version?> GetLatestWingetVersionAsync(CancellationToken cancellationToken = default)
    {
        if (!await CanInstallViaWingetAsync(cancellationToken)) return null;

        var arguments = $"show --id {PandocEnvironment.WingetPackageId} -e --source winget --accept-source-agreements";
        var result = await RunProcessAsync("winget", arguments, ProbeTimeoutMs, cancellationToken);
        if (!result.Started || result.ExitCode != 0)
        {
            Log.Information("winget show failed (started: {Started}, exit code: {ExitCode})",
                result.Started, result.ExitCode);
            return null;
        }

        return PandocEnvironment.ParseWingetShowVersion(result.StandardOutput);
    }

    public Task<bool> InstallAsync(CancellationToken cancellationToken = default) =>
        RunWingetPackageCommandAsync("install", cancellationToken);

    public Task<bool> UpgradeAsync(CancellationToken cancellationToken = default) =>
        RunWingetPackageCommandAsync("upgrade", cancellationToken);

    private async Task<bool> RunWingetPackageCommandAsync(string verb, CancellationToken cancellationToken)
    {
        if (!await CanInstallViaWingetAsync(cancellationToken)) return false;

        var arguments = $"{verb} --id {PandocEnvironment.WingetPackageId} -e --source winget " +
                        "--accept-package-agreements --accept-source-agreements";
        // No timeout: an install/upgrade legitimately takes a while (download + MSI + UAC).
        var result = await RunProcessAsync("winget", arguments, timeoutMs: null, cancellationToken);
        Log.Information("winget {Verb} finished (started: {Started}, exit code: {ExitCode})",
            verb, result.Started, result.ExitCode);
        return result.Started && result.ExitCode == 0;
    }

    private async Task<bool> CanInstallViaWingetAsync(CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows()) return false;
        if (wingetAvailable.HasValue) return wingetAvailable.Value;

        var result = await RunProcessAsync("winget", "--version", ProbeTimeoutMs, cancellationToken);
        wingetAvailable = result.Started && result.ExitCode == 0;
        return wingetAvailable.Value;
    }

    private static async Task<ProcessResult> RunProcessAsync(
        string fileName, string arguments, int? timeoutMs, CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        try
        {
            if (!process.Start())
            {
                return new ProcessResult(false, -1, "", "");
            }
        }
        catch (Win32Exception)
        {
            // Executable not found on PATH (pandoc or winget missing) - report as "not started".
            return new ProcessResult(false, -1, "", "");
        }

        // Drain both streams concurrently before waiting to avoid a full-buffer deadlock.
        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (timeoutMs.HasValue)
        {
            timeoutCts.CancelAfter(timeoutMs.Value);
        }

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
            }
            catch
            {
                // best effort
            }

            return new ProcessResult(false, -1, "", "");
        }

        var standardOutput = await standardOutputTask;
        var standardError = await standardErrorTask;
        return new ProcessResult(true, process.ExitCode, standardOutput, standardError);
    }

    private readonly record struct ProcessResult(bool Started, int ExitCode, string StandardOutput, string StandardError);
}
