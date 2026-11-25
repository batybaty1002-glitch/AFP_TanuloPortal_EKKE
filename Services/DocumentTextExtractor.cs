using System.Text;
using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig;

namespace TanuloPortal.Services;

public static class DocumentTextExtractor
{
    public static async Task<string> ExtractTextAsync(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        using var stream = file.OpenReadStream();

        return extension switch
        {
            ".txt" or ".md" => await ExtractFromTxtAsync(stream),
            ".pdf" => ExtractFromPdf(stream),
            ".pptx" => ExtractFromPptx(stream),
            ".docx" => ExtractFromDocx(stream),
            _ => throw new NotSupportedException($"Nem támogatott fájlformátum: {extension}. Támogatott: .pdf, .pptx, .docx, .txt")
        };
    }

    private static async Task<string> ExtractFromTxtAsync(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    private static string ExtractFromPdf(Stream stream)
    {
        var sb = new StringBuilder();
        using var pdf = PdfDocument.Open(stream);
        foreach (var page in pdf.GetPages())
        {
            sb.AppendLine(page.Text);
        }
        return sb.ToString();
    }

    private static string ExtractFromPptx(Stream stream)
    {
        var sb = new StringBuilder();
        using var presentation = PresentationDocument.Open(stream, false);
        var presentationPart = presentation.PresentationPart;
        if (presentationPart?.Presentation?.SlideIdList == null) return string.Empty;

        foreach (DocumentFormat.OpenXml.Presentation.SlideId slideId in presentationPart.Presentation.SlideIdList)
        {
            if (slideId.RelationshipId?.Value is string relId &&
                presentationPart.GetPartById(relId) is SlidePart slidePart)
            {
                var texts = slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>();
                foreach (var text in texts)
                {
                    sb.Append(text.Text + " ");
                }
                sb.AppendLine();
            }
        }
        return sb.ToString();
    }

    private static string ExtractFromDocx(Stream stream)
    {
        var sb = new StringBuilder();
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body != null)
        {
            sb.Append(body.InnerText);
        }
        return sb.ToString();
    }
}