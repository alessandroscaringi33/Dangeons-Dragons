namespace DndCompanion.Core.Documents;

/// <summary>
/// Reads PDF documents to extract text and metadata. Implementations must
/// never modify the source file.
/// </summary>
public interface IPdfReader
{
    /// <summary>
    /// Reads the PDF at <paramref name="filePath"/> and extracts its text.
    /// </summary>
    /// <returns>
    /// A <see cref="PdfDocumentInfo"/> describing the outcome. Never throws
    /// for missing/corrupted files: those are reported as <see cref="PdfImportStatus.Failed"/>.
    /// </returns>
    Task<PdfDocumentInfo> ExtractAsync(string filePath, CancellationToken cancellationToken = default);
}