using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PandocGui.CliWrapper;
using PandocGui.Services;
using PandocGui.ViewModels;
using PandocGui.Views;
using Serilog;

namespace PandocGui;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
            var dataDirService = new DataDirectoryService();

            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(@$"{dataDirService.GetLogsPath()}/app-logs-{DateTime.Now:yyyy-MM-ddTHH-mm-ss}.log");
#if DEBUG
            loggerConfig.WriteTo.Console();
#endif
            Log.Logger = loggerConfig.CreateLogger();

            desktop.MainWindow.DataContext = new MainWindowViewModel(
                    new FileDialogService(desktop.MainWindow),
                    new PandocCli(),
                    dataDirService,
                    desktop.MainWindow.Clipboard ?? throw new InvalidOperationException("No application clipboard"),
                    new PandocEnvironmentService()
                );
        }

        base.OnFrameworkInitializationCompleted();
    }
}
