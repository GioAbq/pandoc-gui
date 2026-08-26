namespace PandocGui.CliWrapper.Command;

public class PandocCommandGenerator : IPandocCommandGenerator
{
    private readonly string sourceFormat;

    public PandocCommandGenerator(string sourceFormat = PandocFormats.DefaultInputFormat)
    {
        this.sourceFormat = string.IsNullOrWhiteSpace(sourceFormat) ? PandocFormats.DefaultInputFormat : sourceFormat;
    }

    public string GetCommand(string sourcePath)
    {
        return $"-f {sourceFormat} \"{sourcePath}\"";
    }
}
