using System.Buffers.Binary;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EvidenceGenerator.Api.Documents;

public sealed record EvidenceImage(string DataUrl);
public sealed record Evidence(string Id, string Description, List<EvidenceImage> Images);
public sealed record EvidenceSection(string Url, string Connection, EvidenceImage? LoginImage, List<Evidence> Items);
public sealed record MailSettings(string RecipientName, string To, string Cc, string Subject, int FontSize,
    string EnvironmentalNotice, string ConfidentialityNotice, EvidenceImage? Signature, int SignatureWidth = 420, int TemplateVersion = 0, string? BodyHtml = null);
public sealed record EvidenceDocument(Guid Id, int Revision, string Requirement, string Description,
    string Author, string Client, DateOnly Date, EvidenceSection Sql, EvidenceSection Oracle, MailSettings Mail);

public static partial class DocumentValidation
{
    public const int MaxRequestBytes = 35 * 1024 * 1024;
    public static string? Validate(EvidenceDocument doc, bool export = false)
    {
        if (doc.Id == Guid.Empty || doc.Revision < 0) return "Identificador o revisión inválidos.";
        if (doc.Requirement is null || !RequirementPattern().IsMatch(doc.Requirement)) return "El requerimiento debe tener entre 1 y 80 letras, números, espacios, guiones o puntos.";
        if (doc.Description is null || doc.Description.Length > 4000 || doc.Author is null || doc.Author.Length > 150 || doc.Client is null || doc.Client.Length > 150)
            return "La información general supera el tamaño permitido.";
        if (export && (string.IsNullOrWhiteSpace(doc.Author) || string.IsNullOrWhiteSpace(doc.Description))) return "Completa autor y descripción antes de exportar.";
        if (doc.Sql is null || doc.Oracle is null || doc.Mail is null) return "Faltan secciones del documento.";
        var imageCount = 0;
        foreach (var section in new[] { doc.Sql, doc.Oracle })
        {
            if (section.Items is null || section.Items.Count > 50 || section.Connection is null || section.Connection.Length > 150) return "Máximo 50 evidencias por motor.";
            if (!Uri.TryCreate(section.Url, UriKind.Absolute, out var url) || url.Scheme is not ("http" or "https") || section.Url.Length > 2048) return "El sitio debe ser una URL HTTP o HTTPS válida.";
            if (section.LoginImage is not null) { imageCount++; if (!ValidImage(section.LoginImage)) return "Captura de inicio inválida."; }
            foreach (var item in section.Items)
            {
                if (item is null || item.Description is null || item.Description.Length > 4000 || item.Images is null || item.Images.Count > 1) return "Cada evidencia admite 4.000 caracteres y una sola imagen.";
                if (export && string.IsNullOrWhiteSpace(item.Description)) return "Cada evidencia necesita una descripción.";
                foreach (var image in item.Images) { imageCount++; if (!ValidImage(image)) return "Imagen inválida. Usa PNG de hasta 5 MB y 4.096 píxeles por lado."; }
            }
        }
        var mail = doc.Mail;
        if (mail.BodyHtml?.Length > 100_000) return "El cuerpo del correo supera 100.000 caracteres HTML.";
        if (mail.SignatureWidth is < 240 or > 600 || mail.TemplateVersion is < 0 or > 1) return "Ancho de firma o versión de correo inválidos.";
        if (mail.FontSize is not (11 or 12) || new[] { mail.RecipientName, mail.To, mail.Cc, mail.Subject, mail.EnvironmentalNotice, mail.ConfidentialityNotice }.Any(s => s is null || s.Length > 4000)) return "Configuración de correo inválida.";
        if (mail.Signature is not null && !ValidImage(mail.Signature)) return "Firma inválida.";
        if (imageCount > 100) return "Máximo 100 imágenes por documento.";
        if (JsonSerializer.SerializeToUtf8Bytes(doc).Length > MaxRequestBytes - 1024) return "El documento supera 35 MB; reduce las capturas.";
        return null;
    }

    public static bool ValidImage(EvidenceImage? image)
    {
        if (image is null || image.DataUrl is null || !image.DataUrl.StartsWith("data:image/png;base64,", StringComparison.Ordinal) || image.DataUrl.Length > 7_000_000) return false;
        try
        {
            var bytes = Convert.FromBase64String(image.DataUrl[22..]);
            if (bytes.Length is < 33 or > 5_242_880 || !bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) || !bytes.AsSpan(12, 4).SequenceEqual("IHDR"u8)) return false;
            var width = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4));
            var height = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4));
            return width is > 0 and <= 4096 && height is > 0 and <= 4096;
        }
        catch (FormatException) { return false; }
    }

    [GeneratedRegex(@"^[\p{L}\p{N} ._-]{1,80}$")]
    private static partial Regex RequirementPattern();
}
