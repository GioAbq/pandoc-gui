using System.Reactive.Concurrency;
using System.Runtime.CompilerServices;
using ReactiveUI.Builder;

namespace PandocGui.Tests;

/// <summary>
/// The app initializes ReactiveUI through Avalonia (<c>UseReactiveUI</c> in <c>Program</c>), which never
/// runs in a test host, and ReactiveUI throws on first use when it has not been initialized. This does the
/// same job for the test assembly, and pins both schedulers to the immediate one so commands and
/// <c>ToProperty</c> pipelines run synchronously inside a test.
/// </summary>
internal static class ReactiveUiTestHost
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        var builder = RxAppBuilder.CreateReactiveUIBuilder();
        builder.WithMainThreadScheduler(ImmediateScheduler.Instance);
        builder.WithTaskPoolScheduler(ImmediateScheduler.Instance);
        builder.Build();
    }
}
