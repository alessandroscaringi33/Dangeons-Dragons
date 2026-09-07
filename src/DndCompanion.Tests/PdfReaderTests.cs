using DndCompanion.Core.Documents;
using DndCompanion.Infrastructure.Documents;
using Microsoft.Extensions.Logging.Abstractions;

namespace DndCompanion.Tests;

public sealed class PdfReaderTests
{
    private readonly PdfPigPdfReader _reader = new(NullLogger<PdfPigPdfReader>.Instance);

    private static string CreateTempPdfFile(byte[] content)
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "DndCompanionPdfTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, "documento.pdf");
        File.WriteAllBytes(path, content);
        return path;
    }

    [Fact]
    public async Task ValidPdf_TextIsExtracted()
    {
        var path = CreateTempPdfFile(TestPdfBuilder.CreateTextPdf("Benvenuti nella taverna del Drago Rosso"));

        var result = await _reader.ExtractAsync(path);

        Assert.Equal(PdfImportStatus.Imported, result.Status);
        Assert.Equal(1, result.PageCount);
        Assert.NotNull(result.ExtractedText);
        Assert.Contains("Benvenuti nella taverna del Drago Rosso", result.ExtractedText);
    }

    [Fact]
    public async Task ValidPdf_FileNameAndPathAreReported()
    {
        var path = CreateTempPdfFile(TestPdfBuilder.CreateTextPdf("testo"));

        var result = await _reader.ExtractAsync(path);

        Assert.Equal("documento.pdf", result.FileName);
        Assert.Equal(path, result.FilePath);
    }

    [Fact]
    public async Task PdfWithStrangeCharacters_TextIsExtracted()
    {
        var text = "Città — Potèro «strano» € à è é ò ù";
        var path = CreateTempPdfFile(TestPdfBuilder.CreateTextPdf(text));

        var result = await _reader.ExtractAsync(path);

        Assert.Equal(PdfImportStatus.Imported, result.Status);
        Assert.NotNull(result.ExtractedText);
        Assert.Contains("Potèro", result.ExtractedText);
    }

    [Fact]
    public async Task CorruptedPdf_Fails_WithMessage()
    {
        var path = CreateTempPdfFile(TestPdfBuilder.CreateCorruptedPdf());

        var result = await _reader.ExtractAsync(path);

        Assert.Equal(PdfImportStatus.Failed, result.Status);
        Assert.NotNull(result.Message);
        Assert.False(string.IsNullOrWhiteSpace(result.Message));
    }

    [Fact]
    public async Task EmptyPdf_IsReportedAsFailed()
    {
        var path = CreateTempPdfFile(TestPdfBuilder.CreateEmptyPdf());

        var result = await _reader.ExtractAsync(path);

        Assert.Equal(PdfImportStatus.Failed, result.Status);
        Assert.Contains("non contiene pagine", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task NoTextPdf_IsReportedAsNoText()
    {
        var path = CreateTempPdfFile(TestPdfBuilder.CreateNoTextPdf());

        var result = await _reader.ExtractAsync(path);

        Assert.Equal(PdfImportStatus.NoText, result.Status);
        Assert.Equal(1, result.PageCount);
        Assert.Null(result.ExtractedText);
        Assert.Contains("testo estraibile", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MissingFile_Fails_WithMessage()
    {
        var path = Path.Combine(Path.GetTempPath(), "non-esiste.pdf");

        var result = await _reader.ExtractAsync(path);

        Assert.Equal(PdfImportStatus.Failed, result.Status);
        Assert.Contains("non esiste", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EmptyBytes_IsReportedAsFailed()
    {
        var path = CreateTempPdfFile(Array.Empty<byte>());

        var result = await _reader.ExtractAsync(path);

        Assert.Equal(PdfImportStatus.Failed, result.Status);
    }
}