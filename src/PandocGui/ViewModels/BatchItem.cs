using System.IO;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace PandocGui.ViewModels;

public partial class BatchItem : ReactiveObject
{
    public string SourcePath { get; }
    public string FileName => Path.GetFileName(SourcePath);

    [Reactive] public partial string Status { get; set; }

    public BatchItem(string sourcePath)
    {
        SourcePath = sourcePath;
        Status = "Pending";
    }
}
