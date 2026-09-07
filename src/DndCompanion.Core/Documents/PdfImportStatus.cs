namespace DndCompanion.Core.Documents;

/// <summary>
/// Result state of a PDF text extraction/import attempt.
/// </summary>
public enum PdfImportStatus
{
    /// <summary>The document has not been read yet.</summary>
    NotImported,

    /// <summary>Text was successfully extracted from the document.</summary>
    Imported,

    /// <summary>The document was read but contains no extractable text
    /// (e.g. a scanned PDF).</summary>
    NoText,

    /// <summary>The document could not be read (missing, corrupted or invalid).</summary>
    Failed
}