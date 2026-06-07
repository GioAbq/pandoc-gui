using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using PandocGui.CliWrapper;
using PandocGui.CliWrapper.Command;
using PandocGui.Services;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Serilog;
using SkiaSharp;

namespace PandocGui.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public ReactiveCommand<Unit, Unit> SearchSourceFileCommand { get; }
    public ReactiveCommand<Unit, Unit> ExportCommand { get; }
    public ReactiveCommand<Unit, Unit> SearchTargetFileCommand { get; }
    public ReactiveCommand<Unit, Unit> SearchHighlightThemeSourceCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenLogFolderCommand { get; }
    public ReactiveCommand<Unit, Unit> ClearCommand { get; }
    public ReactiveCommand<Unit, Unit> CopyCommand { get; }
    public ReactiveCommand<Unit, Unit> PandocActionCommand { get; }
    public ReactiveCommand<Unit, Unit> SavePresetCommand { get; }
    public ReactiveCommand<Unit, Unit> DeletePresetCommand { get; }
    public ReactiveCommand<Unit, Unit> ImportPresetCommand { get; }
    public ReactiveCommand<Unit, Unit> ExportPresetCommand { get; }

    [Reactive] public partial string SourcePath { get; set; }

    [Reactive] public partial string TargetPath { get; set; }

    [Reactive] public partial InputFormat SelectedInputFormat { get; set; }
    [Reactive] public partial OutputFormat SelectedOutputFormat { get; set; }

    [Reactive] public partial bool CustomHighlightThemeEnabled { get; set; }

    [Reactive] public partial string CustomHighlightThemeSource { get; set; }

    [Reactive] public partial bool NumberedHeadersEnabled { get; set; }
    [Reactive] public partial bool CustomFontEnabled { get; set; }
    [Reactive] public partial string CustomFontName { get; set; }
    [Reactive] public partial bool CustomMarginEnabled { get; set; }
    [Reactive] public partial decimal CustomMarginValue { get; set; }
    [Reactive] public partial bool CustomPdfEngineEnabled { get; set; }
    [Reactive] public partial string CustomPdfEngineValue { get; set; }
    [Reactive] public partial bool TableOfContentEnabled { get; set; }
    [Reactive] public partial string Result { get; set; }
    [Reactive] public partial bool IsError { get; set; }
    [Reactive] public partial bool OpenFileOnCompletion { get; set; }

    [Reactive] public partial bool IsPandocAvailable { get; set; }
    [Reactive] public partial bool IsPandocBannerVisible { get; set; }
    [Reactive] public partial string PandocBannerTitle { get; set; }
    [Reactive] public partial string PandocBannerMessage { get; set; }
    [Reactive] public partial bool IsPandocBannerError { get; set; }
    [Reactive] public partial bool IsPandocActionVisible { get; set; }
    [Reactive] public partial string PandocActionLabel { get; set; }

    [Reactive] public partial Preset? SelectedPreset { get; set; }
    [Reactive] public partial string NewPresetName { get; set; }
    public ObservableCollection<Preset> Presets { get; } = new();

    public List<string> SupportedEngine { get; } = PdfEnginePandocCommandGenerator.supportedEngines.ToList();
    public List<string> InstalledFonts { get; }
    public List<InputFormat> SupportedInputFormats { get; } = PandocFormats.InputFormats.ToList();
    public List<OutputFormat> SupportedOutputFormats { get; } = PandocFormats.OutputFormats.ToList();


    private readonly IFileDialogService fileDialogService;
    private readonly IPandocCli pandoc;
    private readonly IDataDirectoryService dataDirectoryService;
    private readonly IClipboard clipboard;
    private readonly IPandocEnvironmentService pandocEnvironment;
    private readonly IPresetService presetService;
    private PandocStatus? lastStatus;
    private readonly ObservableAsPropertyHelper<bool> isExporting;
    public bool IsExporting => isExporting.Value;
    private readonly ObservableAsPropertyHelper<bool> hasInput;
    public bool HasInput => hasInput.Value;
    private readonly ObservableAsPropertyHelper<bool> isStatusVisible;
    public bool IsStatusVisible => isStatusVisible.Value;

    public MainWindowViewModel(IFileDialogService fileDialogService, IPandocCli pandoc,
        IDataDirectoryService dataDirectoryService, IClipboard clipboard,
        IPandocEnvironmentService pandocEnvironment, IPresetService presetService)
    {
        this.clipboard = clipboard;
        dataDirectoryService.EnsureCreated();
        SourcePath = "";
        TargetPath = "";
        CustomHighlightThemeSource = "";
        CustomFontName = "";
        CustomMarginValue = 1.3m;
        CustomPdfEngineValue = "";
        Result = "";
        OpenFileOnCompletion = true;
        PandocBannerTitle = "";
        PandocBannerMessage = "";
        PandocActionLabel = "";
        NewPresetName = "";
        SelectedInputFormat = SupportedInputFormats[0];
        SelectedOutputFormat = SupportedOutputFormats[0];
        this.fileDialogService = fileDialogService;
        this.pandoc = pandoc;
        this.dataDirectoryService = dataDirectoryService;
        this.pandocEnvironment = pandocEnvironment;
        this.presetService = presetService;
        ClearCommand = ReactiveCommand.CreateFromTask(Clear);
        SearchSourceFileCommand = ReactiveCommand.CreateFromTask(SearchInputFile);
        SearchTargetFileCommand = ReactiveCommand.CreateFromTask(SearchOutputFile);

        var canExport = this.WhenAnyValue(
            x => x.SourcePath,
            x => x.TargetPath,
            x => x.IsPandocAvailable,
            (source, target, pandocAvailable) => pandocAvailable
                && !string.IsNullOrWhiteSpace(source) && !string.IsNullOrWhiteSpace(target));
        ExportCommand = ReactiveCommand.CreateFromTask(Export, canExport);
        CopyCommand = ReactiveCommand.CreateFromTask(CopyPandocToClipBoard);
        ExportCommand.IsExecuting.ToProperty(this, x => x.IsExporting, out isExporting);
        SearchHighlightThemeSourceCommand = ReactiveCommand.CreateFromTask(SearchHighlightThemeSource);
        OpenLogFolderCommand = ReactiveCommand.Create(dataDirectoryService.OpenLogFolder);
        PandocActionCommand = ReactiveCommand.CreateFromTask(RunPandocActionAsync);
        PandocActionCommand.ThrownExceptions.Subscribe(ex => Log.Error(ex, "Pandoc action failed"));

        var canSavePreset = this.WhenAnyValue(x => x.NewPresetName)
            .Select(name => !string.IsNullOrWhiteSpace(name));
        var hasSelectedPreset = this.WhenAnyValue(x => x.SelectedPreset)
            .Select(preset => preset is not null);
        SavePresetCommand = ReactiveCommand.CreateFromTask(SavePreset, canSavePreset);
        DeletePresetCommand = ReactiveCommand.CreateFromTask(DeletePreset, hasSelectedPreset);
        ImportPresetCommand = ReactiveCommand.CreateFromTask(ImportPreset);
        ExportPresetCommand = ReactiveCommand.CreateFromTask(ExportPreset, hasSelectedPreset);
        Observable.Merge(
                SavePresetCommand.ThrownExceptions,
                DeletePresetCommand.ThrownExceptions,
                ImportPresetCommand.ThrownExceptions,
                ExportPresetCommand.ThrownExceptions)
            .Subscribe(ex =>
            {
                Log.Error(ex, "Preset operation failed");
                IsError = true;
                Result = ex.Message;
            });

        this.WhenAnyValue(x => x.SelectedPreset)
            .WhereNotNull()
            .Subscribe(ApplyPreset);

        ReloadPresets();

        this.WhenAnyValue(x => x.SourcePath)
            .Select(path => !string.IsNullOrWhiteSpace(path))
            .ToProperty(this, x => x.HasInput, out hasInput);
        this.WhenAnyValue(x => x.Result)
            .Select(result => !string.IsNullOrWhiteSpace(result))
            .ToProperty(this, x => x.IsStatusVisible, out isStatusVisible);

        this.WhenAnyValue(x => x.SourcePath)
            .Subscribe(path =>
            {
                if (string.IsNullOrWhiteSpace(path)) return;
                var detected = PandocFormats.DetectInputFormat(path);
                SelectedInputFormat = SupportedInputFormats.FirstOrDefault(format => format.Format == detected)
                                      ?? SupportedInputFormats[0];
                SetTargetFromSource();
            });
        this.WhenAnyValue(x => x.SelectedOutputFormat)
            .Skip(1)
            .Subscribe(_ => SwapTargetExtension());

        InstalledFonts = GetInstalledFonts();

        //auto set SourcePath as the file user opened.
        var args = Environment.GetCommandLineArgs();
        if (args.Count() > 1 && File.Exists(args[1]))
        {
            this.SourcePath = args[1];
        }

        // Detect pandoc at startup (fire-and-forget; exceptions handled inside).
        _ = CheckPandocEnvironmentAsync();
    }

    private async Task CheckPandocEnvironmentAsync()
    {
        try
        {
            // Apply the local detection result first so Convert unblocks immediately;
            // the winget update check (network, slower) must not gate the UI.
            var status = await pandocEnvironment.DetectAsync();
            Dispatcher.UIThread.Post(() => ApplyStatus(status));

            if (status.Availability == PandocAvailability.Installed && status.CanInstallViaWinget)
            {
                var latest = await pandocEnvironment.GetLatestWingetVersionAsync();
                var withUpdate = status with
                {
                    LatestVersion = latest,
                    UpdateAvailable = PandocEnvironment.IsUpdateAvailable(status.InstalledVersion, latest)
                };
                Dispatcher.UIThread.Post(() => ApplyStatus(withUpdate));
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to check the pandoc environment");
        }
    }

    private void ApplyStatus(PandocStatus status)
    {
        lastStatus = status;
        IsPandocAvailable = status.Availability == PandocAvailability.Installed;

        if (status.Availability == PandocAvailability.NotInstalled)
        {
            IsPandocBannerError = true;
            PandocBannerTitle = "Pandoc not found";
            PandocBannerMessage = status.CanInstallViaWinget
                ? "Pandoc is required to convert documents. Install it to continue."
                : status.ManualInstructions
                  ?? PandocEnvironment.GetManualInstallInstructions(PandocEnvironment.GetCurrentOs());
            PandocActionLabel = status.CanInstallViaWinget ? "Install pandoc" : "Open download page";
            IsPandocActionVisible = true;
            IsPandocBannerVisible = true;
            return;
        }

        if (status.Availability == PandocAvailability.Installed && status.UpdateAvailable)
        {
            IsPandocBannerError = false;
            PandocBannerTitle = "Update available";
            PandocBannerMessage = $"Pandoc {status.LatestVersion} is available (you have {status.InstalledVersion}).";
            PandocActionLabel = "Update";
            IsPandocActionVisible = status.CanInstallViaWinget;
            IsPandocBannerVisible = true;
            return;
        }

        // Installed and current (or version unknown) - nothing to show.
        IsPandocBannerVisible = false;
        IsPandocActionVisible = false;
    }

    private async Task RunPandocActionAsync()
    {
        var status = lastStatus;
        if (status is null) return;

        if (status.Availability == PandocAvailability.NotInstalled && status.CanInstallViaWinget)
        {
            await pandocEnvironment.InstallAsync();
            await CheckPandocEnvironmentAsync();
            return;
        }

        if (status.UpdateAvailable && status.CanInstallViaWinget)
        {
            await pandocEnvironment.UpgradeAsync();
            await CheckPandocEnvironmentAsync();
            return;
        }

        OpenUrl(PandocEnvironment.DownloadUrl);
    }

    private static void OpenUrl(string url)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            else if (OperatingSystem.IsLinux())
            {
                Process.Start(new ProcessStartInfo { FileName = "xdg-open", Arguments = url, UseShellExecute = false });
            }
            else if (OperatingSystem.IsMacOS())
            {
                Process.Start(new ProcessStartInfo { FileName = "open", Arguments = url, UseShellExecute = false });
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to open URL {Url}", url);
        }
    }

    private void SetTargetFromSource()
    {
        if (string.IsNullOrWhiteSpace(SourcePath)) return;
        var directory = Path.GetDirectoryName(SourcePath) ?? "";
        var name = Path.GetFileNameWithoutExtension(SourcePath);
        TargetPath = Path.Combine(directory, name + SelectedOutputFormat.Extension);
    }

    private void SwapTargetExtension()
    {
        if (string.IsNullOrWhiteSpace(TargetPath))
        {
            SetTargetFromSource();
            return;
        }

        var directory = Path.GetDirectoryName(TargetPath) ?? "";
        var name = Path.GetFileNameWithoutExtension(TargetPath);
        TargetPath = Path.Combine(directory, name + SelectedOutputFormat.Extension);
    }

    private static List<string> GetInstalledFonts() => SKFontManager.Default.FontFamilies.ToList();

    private async Task SearchHighlightThemeSource()
    {
        CustomHighlightThemeSource = await fileDialogService.OpenFileAsync();
    }

    private async Task Export()
    {
        try
        {
            this.Result = "";
            await this.pandoc.ExportPdfAsync(BuildPandocParameters());
            this.IsError = false;
            this.Result = "Success";

            if (OpenFileOnCompletion)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "xdg-open" : TargetPath,
                    Arguments = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? TargetPath : "",
                    UseShellExecute = !RuntimeInformation.IsOSPlatform(OSPlatform.Linux),
                    CreateNoWindow = true,
                    WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                });
            }
        }
        catch (Exception e)
        {
            await Console.Error.WriteLineAsync(e.Message);
            this.IsError = true;
            this.Result = e.Message;
        }
    }

    private PandocParameters BuildPandocParameters()
    {
        return new PandocParameters()
        {
            SourcePath = SourcePath,
            TargetPath = TargetPath,
            SourceFormat = SelectedInputFormat.Format,
            HighlightTheme = CustomHighlightThemeEnabled,
            HighlightThemeSource = CustomHighlightThemeSource,
            NumberedHeader = NumberedHeadersEnabled,
            CustomFont = CustomFontEnabled,
            CustomFontName = CustomFontName,
            CustomMargin = CustomMarginEnabled,
            CustomMarginValue = CustomMarginValue,
            CustomPdfEngine = CustomPdfEngineEnabled,
            CustomPdfEngineValue = CustomPdfEngineValue,
            TableOfContents = TableOfContentEnabled,
            LogToFile = true,
            LogFilePath =
                @$"{dataDirectoryService.GetLogsPath()}/pandoc-logs-{DateTime.Now:yyyy-MM-ddTHH-mm-ss}.json"
        };
    }

    private async Task SearchInputFile()
    {
        var groups = PandocFormats.InputFormats
            .Where(format => format.Extensions.Count > 0)
            .GroupBy(format => format.Category)
            .Select(group => new FilePickerGroup(
                group.Key,
                group.SelectMany(format => format.Extensions).Distinct().ToList()))
            .ToList();

        SourcePath = await fileDialogService.OpenFileAsync(groups);
        Console.WriteLine($"Source path : {SourcePath}");
    }

    private async Task SearchOutputFile()
    {
        TargetPath = await fileDialogService.SaveFileAsync();
        Console.WriteLine($"Source path : {TargetPath}");
    }

    private Task Clear()
    {
        SourcePath = "";
        TargetPath = "";
        SelectedInputFormat = SupportedInputFormats[0];
        SelectedOutputFormat = SupportedOutputFormats[0];
        Result = "";
        IsError = false;

        CustomHighlightThemeEnabled = false;
        CustomHighlightThemeSource = "";
        NumberedHeadersEnabled = false;
        CustomFontEnabled = false;
        CustomFontName = "";
        CustomMarginEnabled = false;
        CustomMarginValue = 1.3m;
        CustomPdfEngineEnabled = false;
        CustomPdfEngineValue = "";
        TableOfContentEnabled = false;
        return Task.CompletedTask;
    }

    private Task CopyPandocToClipBoard()
    {
        clipboard.SetTextAsync($"pandoc {pandoc.GetCommand(BuildPandocParameters())}");
        return Task.CompletedTask;
    }

    private void ReloadPresets()
    {
        Presets.Clear();
        foreach (var preset in presetService.LoadAll())
        {
            Presets.Add(preset);
        }
    }

    private void ApplyPreset(Preset preset)
    {
        SelectedOutputFormat = SupportedOutputFormats.FirstOrDefault(format => format.Extension == preset.OutputExtension)
                               ?? SupportedOutputFormats[0];
        CustomHighlightThemeEnabled = preset.HighlightTheme;
        CustomHighlightThemeSource = preset.HighlightThemeSource;
        NumberedHeadersEnabled = preset.NumberedHeader;
        CustomFontEnabled = preset.CustomFont;
        CustomFontName = preset.CustomFontName;
        CustomMarginEnabled = preset.CustomMargin;
        CustomMarginValue = preset.CustomMarginValue;
        CustomPdfEngineEnabled = preset.CustomPdfEngine;
        CustomPdfEngineValue = preset.CustomPdfEngineValue;
        TableOfContentEnabled = preset.TableOfContents;
    }

    private Preset BuildPreset(string name) => new()
    {
        Name = name,
        OutputExtension = SelectedOutputFormat.Extension,
        HighlightTheme = CustomHighlightThemeEnabled,
        HighlightThemeSource = CustomHighlightThemeSource,
        NumberedHeader = NumberedHeadersEnabled,
        CustomFont = CustomFontEnabled,
        CustomFontName = CustomFontName,
        CustomMargin = CustomMarginEnabled,
        CustomMarginValue = CustomMarginValue,
        CustomPdfEngine = CustomPdfEngineEnabled,
        CustomPdfEngineValue = CustomPdfEngineValue,
        TableOfContents = TableOfContentEnabled
    };

    private Task SavePreset()
    {
        var name = (NewPresetName ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name)) return Task.CompletedTask;

        presetService.Save(BuildPreset(name));
        ReloadPresets();
        SelectedPreset = Presets.FirstOrDefault(preset => preset.Name == name);
        NewPresetName = "";
        return Task.CompletedTask;
    }

    private Task DeletePreset()
    {
        if (SelectedPreset is null) return Task.CompletedTask;

        presetService.Delete(SelectedPreset.Name);
        SelectedPreset = null;
        ReloadPresets();
        return Task.CompletedTask;
    }

    private async Task ImportPreset()
    {
        var path = await fileDialogService.OpenFileAsync();
        if (string.IsNullOrWhiteSpace(path)) return;

        var preset = presetService.Import(path);
        presetService.Save(preset);
        ReloadPresets();
        SelectedPreset = Presets.FirstOrDefault(existing => existing.Name == preset.Name);
    }

    private async Task ExportPreset()
    {
        if (SelectedPreset is null) return;

        var path = await fileDialogService.SaveFileAsync();
        if (string.IsNullOrWhiteSpace(path)) return;

        presetService.Export(SelectedPreset, path);
    }
}