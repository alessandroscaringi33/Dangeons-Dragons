using System.Text;
using DndCompanion.Core.Documents;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;

namespace DndCompanion.Infrastructure.Documents;

/// <summary>
/// <see cref="IPdfReader"/> implementation backed by PdfPig. Extracts text and
/// page count from a PDF without modifying the file.
/// </summary>
public sealed class PdfPigPdfReader : IPdfReader
{
    private readonly ILogger<PdfPigPdfReader> _logger;

    public PdfPigPdfReader(ILogger<PdfPigPdfReader> logger)
    {
        _logger = logger;
    }

    public async Task<PdfDocumentInfo> ExtractAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            return PdfDocumentInfo.Failed(filePath, "Il file PDF non esiste.");
        }

        try
        {
            return await Task.Run(() => ExtractCore(filePath, cancellationToken), cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while reading PDF '{Path}'", filePath);
            return PdfDocumentInfo.Failed(filePath, "Non è stato possibile leggere il PDF.");
        }
    }

    private PdfDocumentInfo ExtractCore(string filePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using var document = PdfDocument.Open(filePath);

            var pageCount = document.NumberOfPages;
            if (pageCount <= 0)
            {
                return PdfDocumentInfo.Failed(filePath, "Il PDF non contiene pagine.");
            }

            var builder = new StringBuilder();
            foreach (var page in document.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();
                builder.AppendLine(page.Text);
            }

            var text = builder.ToString().Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return PdfDocumentInfo.NoText(
                    filePath,
                    pageCount,
                    "Il PDF non contiene testo estraibile: potrebbe essere un documento scansionato.");
            }

            return PdfDocumentInfo.Imported(filePath, pageCount, text);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract text from PDF '{Path}'", filePath);
            return PdfDocumentInfo.Failed(filePath, "Il file non è un PDF valido o è corrotto.");
        }
    }
}