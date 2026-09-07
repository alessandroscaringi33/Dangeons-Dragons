using System.Text;

namespace DndCompanion.Tests;

/// <summary>
/// Builds minimal but valid PDF files in memory for testing the PDF reader.
/// Never touches the real <c>Campagne</c> folders.
/// </summary>
public static class TestPdfBuilder
{
    private static readonly Encoding WinAnsi;

    static TestPdfBuilder()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        WinAnsi = Encoding.GetEncoding(1252);
    }

    /// <summary>A one-page PDF containing the given text.</summary>
    public static byte[] CreateTextPdf(string text)
    {
        var content = $"BT /F1 12 Tf 72 720 Td ({Escape(text)}) Tj ET";
        var contentLength = WinAnsi.GetByteCount(content);
        var contentObject = $"<< /Length {contentLength} >>\nstream\n{content}\nendstream";

        return Build(new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>",
            contentObject,
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"
        });
    }

    /// <summary>A PDF with zero pages.</summary>
    public static byte[] CreateEmptyPdf()
    {
        return Build(new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [] /Count 0 >>"
        });
    }

    /// <summary>A PDF whose page contains no text (simulates a scanned page).</summary>
    public static byte[] CreateNoTextPdf()
    {
        const string content = "q Q";
        var contentObject = $"<< /Length {content.Length} >>\nstream\n{content}\nendstream";

        return Build(new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R >>",
            contentObject
        });
    }

    /// <summary>Bytes that look like a PDF but are not valid.</summary>
    public static byte[] CreateCorruptedPdf()
    {
        return WinAnsi.GetBytes("%PDF-1.4\nthis is not a real pdf, just garbage\n%%EOF");
    }

    private static string Escape(string text)
    {
        return text
            .Replace("\\", "\\\\")
            .Replace("(", "\\(")
            .Replace(")", "\\)");
    }

    private static byte[] Build(string[] objectBodies)
    {
        var bytes = new List<byte>();

        void Write(string value) => bytes.AddRange(WinAnsi.GetBytes(value));

        Write("%PDF-1.4\n");

        var offsets = new long[objectBodies.Length];
        for (var i = 0; i < objectBodies.Length; i++)
        {
            offsets[i] = bytes.Count;
            Write($"{i + 1} 0 obj\n{objectBodies[i]}\nendobj\n");
        }

        var xrefOffset = bytes.Count;
        Write("xref\n");
        Write($"0 {objectBodies.Length + 1}\n");
        Write("0000000000 65535 f \n");
        foreach (var offset in offsets)
        {
            Write($"{offset:0000000000} 00000 n \n");
        }

        Write("trailer\n");
        Write($"<< /Size {objectBodies.Length + 1} /Root 1 0 R >>\n");
        Write("startxref\n");
        Write($"{xrefOffset}\n");
        Write("%%EOF\n");

        return bytes.ToArray();
    }
}