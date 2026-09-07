namespace DndCompanion.Core.Documents;

/// <summary>
/// The outcome of reading a PDF document: metadata, extracted text (when
/// available) and a human readable message.
/// </summary>
public sealed class PdfDocumentInfo
{
    public string FilePath { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    /// <summary>Number of pages, or 0 when it could not be determined.</summary>
    public int PageCount { get; init; }

    public PdfImportStatus Status { get; init; }

    /// <summary>Extracted text, or null when no text could be extracted.</summary>
    public string? ExtractedText { get; init; }

    /// <summary>Human readable detail describing the outcome.</summary>
    public string? Message { get; init; }

    public static PdfDocumentInfo Imported(string filePath, int pageCount, string extractedText)
    {
        return new PdfDocumentInfo
        {
            FilePath = filePath,
            FileName = Path.GetFileName(filePath),
            PageCount = pageCount,
            Status = PdfImportStatus.Imported,
            ExtractedText = extractedText
        };
    }

    public static PdfDocumentInfo NoText(string filePath, int pageCount, string message)
    {
        return new PdfDocumentInfo
        {
            FilePath = filePath,
            FileName = Path.GetFileName(filePath),
            PageCount = pageCount,
            Status = PdfImportStatus.NoText,
            Message = message
        };
    }

    public static PdfDocumentInfo Failed(string filePath, string message)
    {
        return new PdfDocumentInfo
        {
            FilePath = filePath,
            FileName = Path.GetFileName(filePath),
            PageCount = 0,
            Status = PdfImportStatus.Failed,
            Message = message
        };
    }
}