#nullable enable
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using DocSharp.Binary.DocFileFormat;
using DocSharp.Binary.OpenXmlLib;
using DocSharp.Binary.OpenXmlLib.WordprocessingML;
using DocSharp.Binary.StructuredStorage.Reader;
using DocSharp.Binary.WordprocessingMLMapping;
using Serilog;

namespace PandocGui.CliWrapper;

/// <summary>
/// Cascading <c>.doc</c> to <c>.docx</c> converter: Microsoft Word via COM (best fidelity, Windows
/// only when Office is installed), then the pure-managed DocSharp reader, then a clear error.
/// </summary>
public class DocPreprocessorService : IDocPreprocessor
{
    public Task<string> ConvertToDocxAsync(string docPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(docPath))
        {
            throw new FileNotFoundException("The source document could not be found.", docPath);
        }

        var docxPath = Path.Combine(Path.GetTempPath(), $"pandoc-gui-{Guid.NewGuid():N}.docx");

        // The conversion is synchronous and CPU/COM-bound; keep it off the calling (UI) thread.
        return Task.Run(() =>
        {
            if (OperatingSystem.IsWindows() && TryConvertWithWord(docPath, docxPath))
            {
                Log.Information("Converted .doc to .docx via Word COM: {DocxPath}", docxPath);
                return docxPath;
            }

            ConvertWithDocSharp(docPath, docxPath);
            Log.Information("Converted .doc to .docx via DocSharp: {DocxPath}", docxPath);
            return docxPath;
        }, cancellationToken);
    }

    internal static void ConvertWithDocSharp(string docPath, string docxPath)
    {
        try
        {
            if (File.Exists(docxPath))
            {
                File.Delete(docxPath);
            }

            using var reader = new StructuredStorageReader(docPath);
            var doc = new WordDocument(reader);
            using var docx = WordprocessingDocument.Create(docxPath, WordprocessingDocumentType.Document);
            Converter.Convert(doc, docx);
        }
        catch (Exception e)
        {
            Log.Error(e, "DocSharp failed to convert .doc to .docx");
            throw new InvalidOperationException(
                "This .doc file could not be read. Install Microsoft Word, or re-save the document "
                + "as .docx and try again.");
        }
    }

    [SupportedOSPlatform("windows")]
    private static bool TryConvertWithWord(string docPath, string docxPath)
    {
        var wordType = Type.GetTypeFromProgID("Word.Application");
        if (wordType is null)
        {
            return false; // Word is not installed - fall back to DocSharp.
        }

        object? word = null;
        object? doc = null;
        try
        {
            word = Activator.CreateInstance(wordType);
            if (word is null)
            {
                return false;
            }

            dynamic app = word;
            app.Visible = false;
            app.DisplayAlerts = 0; // wdAlertsNone

            doc = app.Documents.Open(docPath);
            const int wdFormatXMLDocument = 12;
            ((dynamic)doc).SaveAs2(docxPath, wdFormatXMLDocument);

            return File.Exists(docxPath);
        }
        catch (Exception e)
        {
            Log.Warning(e, "Word COM conversion failed; falling back to DocSharp");
            return false;
        }
        finally
        {
            try
            {
                if (doc is not null)
                {
                    ((dynamic)doc).Close(false);
                }
            }
            catch
            {
                // best effort
            }

            try
            {
                if (word is not null)
                {
                    ((dynamic)word).Quit();
                }
            }
            catch
            {
                // best effort
            }

            if (doc is not null)
            {
                Marshal.ReleaseComObject(doc);
            }

            if (word is not null)
            {
                Marshal.ReleaseComObject(word);
            }
        }
    }
}
