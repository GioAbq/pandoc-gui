namespace PandocGui.CliWrapper;

/// <summary>
/// Maps pandoc process exit codes to actionable, human-readable messages.
/// Pandoc surfaces failures as numeric exit codes (see <see cref="PandocErrorCode"/>);
/// showing the raw enum name to the user is cryptic, so this translates the most
/// common cases - especially a missing PDF engine - into guidance.
/// </summary>
public static class PandocErrorMessages
{
    public static string Describe(PandocErrorCode error) => error switch
    {
        PandocErrorCode.PandocPDFProgramNotFoundError =>
            "PDF output needs a PDF engine, but none was found. Install a LaTeX distribution "
            + "(e.g. MiKTeX or TeX Live), or pick a PDF engine you already have (e.g. wkhtmltopdf).",

        PandocErrorCode.PandocPDFError or PandocErrorCode.PandocMakePDFError =>
            "The PDF engine failed while producing the document. Check that your LaTeX "
            + "distribution is complete and that the document has no unsupported content.",

        PandocErrorCode.PandocUnknownReaderError =>
            "Pandoc does not recognise the selected input format. Choose a different source format.",

        PandocErrorCode.PandocUnknownWriterError =>
            "Pandoc does not recognise the selected output format. Choose a different target format.",

        PandocErrorCode.PandocUnsupportedExtensionError =>
            "The chosen format does not support one of the requested extensions or options.",

        PandocErrorCode.PandocOptionError =>
            "Pandoc rejected one of the conversion options. Review the selected settings and try again.",

        PandocErrorCode.PandocParseError or PandocErrorCode.PandocParsecError =>
            "Pandoc could not parse the source document. Check that the file is valid and not corrupted.",

        PandocErrorCode.PandocUTF8DecodingError =>
            "The source file is not valid UTF-8 text. Re-save it with UTF-8 encoding and try again.",

        PandocErrorCode.PandocCouldNotFindDataFileError or PandocErrorCode.PandocResourceNotFound =>
            "Pandoc could not find a file the document refers to (e.g. an image or template). "
            + "Check that all referenced resources exist.",

        _ => $"Pandoc failed with error {(int)error} ({error}).",
    };
}
