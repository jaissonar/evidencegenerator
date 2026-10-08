using System.Net.Mail;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace EvidenceGenerator.Api.Documents;

public sealed record Contact(string Id, string Name, string Email);
public sealed record TestEnvironment(string Id, string Name, string Engine, string Url, string Connection);
public sealed record WorkspaceSettings(int Revision, string Name, EvidenceImage? Photo, string ExcelDirectory,
    string SaveMode, EvidenceImage? Signature, int SignatureWidth, List<Contact> Contacts, List<TestEnvironment>? Environments = null,
    List<string>? EnvironmentUrls = null, List<string>? Connections = null);
public sealed record ExportLocation(string Requirement, string Path, DateTimeOffset SavedAt);

// Local preferences share the draft database and its backup lifecycle.
public sealed class WorkspaceStore
{
    private readonly string connectionString;
    private readonly Func<string> downloadsDirectory;
    private readonly object gate = new();
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public WorkspaceStore(DocumentStore documents, Func<string>? downloadsDirectory = null)
    {
        this.downloadsDirectory = downloadsDirectory ?? SystemFolders.Downloads;
        connectionString = new SqliteConnectionStringBuilder { DataSource = documents.DatabasePath }.ToString();
        using var connection = Open(); using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS WorkspaceSettings (Id INTEGER PRIMARY KEY CHECK(Id=1), Revision INTEGER NOT NULL, Payload TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS ExcelLocation (Requirement TEXT PRIMARY KEY COLLATE NOCASE, Payload TEXT NOT NULL);
            """;
        command.ExecuteNonQuery();
    }
    private SqliteConnection Open() { var connection = new SqliteConnection(connectionString); connection.Open(); return connection; }
    public WorkspaceSettings Get()
    {
        using var connection = Open(); using var command = connection.CreateCommand();
        command.CommandText = "SELECT Payload FROM WorkspaceSettings WHERE Id=1";
        var settings = Normalize(command.ExecuteScalar() is string json ? JsonSerializer.Deserialize<WorkspaceSettings>(json, Json)!
            : new(0, "", null, "", "ask", null, 420, [], []));
        return settings with { ExcelDirectory = ResolveDirectory(settings.ExcelDirectory) };
    }
    public string ResolveDirectory(string? directory) => string.IsNullOrWhiteSpace(directory)
        ? downloadsDirectory() : Path.GetFullPath(directory.Trim());
    // Migrate legacy pairs only when the independent catalogs are absent.
    public static WorkspaceSettings Normalize(WorkspaceSettings s) => s with {
        EnvironmentUrls = s.EnvironmentUrls ?? (s.Environments ?? []).Select(e => e.Url).Distinct(StringComparer.Ordinal).ToList(),
        Connections = s.Connections ?? (s.Environments ?? []).Select(e => e.Connection).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
        Environments = null
    };
    public WorkspaceSettings? Save(WorkspaceSettings settings)
    {
        var saved = Normalize(settings) with { Revision = settings.Revision + 1, ExcelDirectory = ResolveDirectory(settings.ExcelDirectory) };
        using var connection = Open(); using var command = connection.CreateCommand();
        command.CommandText = settings.Revision == 0
            ? "INSERT OR IGNORE INTO WorkspaceSettings VALUES(1,$revision,$payload)"
            : "UPDATE WorkspaceSettings SET Revision=$revision,Payload=$payload WHERE Id=1 AND Revision=$previous";
        command.Parameters.AddWithValue("$revision", saved.Revision);
        command.Parameters.AddWithValue("$previous", settings.Revision);
        command.Parameters.AddWithValue("$payload", JsonSerializer.Serialize(saved, Json));
        return command.ExecuteNonQuery() == 1 ? saved : null;
    }
    public static string? Validate(WorkspaceSettings s)
    {
        if (s.Revision < 0 || string.IsNullOrWhiteSpace(s.Name) || s.Name.Length > 150) return "Indica tu nombre (hasta 150 caracteres).";
        if ((!string.IsNullOrWhiteSpace(s.ExcelDirectory) && !ValidDirectory(s.ExcelDirectory)) || s.SaveMode is not ("ask" or "configured")) return "Indica una carpeta local absoluta y un modo de guardado válido.";
        if (s.SignatureWidth is < 240 or > 600 || s.Photo is not null && !DocumentValidation.ValidImage(s.Photo) || s.Signature is not null && !DocumentValidation.ValidImage(s.Signature)) return "Foto, firma o ancho inválidos.";
        if (s.Contacts is null || s.Contacts.Count > 200 || s.Contacts.Any(c => c is null || string.IsNullOrWhiteSpace(c.Id) || c.Id.Length > 80 || string.IsNullOrWhiteSpace(c.Name) || c.Name.Length > 150 || c.Email is null || c.Email.Length > 254 || !MailAddress.TryCreate(c.Email, out var address) || address.Address != c.Email)) return "Revisa los contactos: nombre y correo válido, máximo 200.";
        if (s.Contacts.Select(c => c.Email).Distinct(StringComparer.OrdinalIgnoreCase).Count() != s.Contacts.Count || s.Contacts.Select(c => c.Id).Distinct().Count() != s.Contacts.Count) return "Hay contactos duplicados.";
        if (s.Environments?.Any(e => e is null) == true) return "Catálogo de ambientes inválido.";
        s = Normalize(s);
        if (s.EnvironmentUrls!.Count > 100 || s.EnvironmentUrls.Any(u => string.IsNullOrWhiteSpace(u) || u != u.Trim() || u.Length > 2048 || !Uri.TryCreate(u, UriKind.Absolute, out var url) || url.Scheme is not ("http" or "https"))) return "Agrega hasta 100 URL HTTP/HTTPS válidas, sin espacios exteriores.";
        if (s.Connections!.Count > 100 || s.Connections.Any(c => string.IsNullOrWhiteSpace(c) || c != c.Trim() || c.Length > 150)) return "Agrega hasta 100 conexiones de 1 a 150 caracteres, sin espacios exteriores.";
        if (s.EnvironmentUrls.Distinct(StringComparer.Ordinal).Count() != s.EnvironmentUrls.Count || s.Connections.Distinct(StringComparer.OrdinalIgnoreCase).Count() != s.Connections.Count) return "Hay URL o conexiones duplicadas.";
        return null;
    }
    public static bool ValidDirectory(string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || directory.Length > 220) return false;
        try { return Path.IsPathFullyQualified(directory.Trim()) && !directory.Trim().StartsWith(@"\\") && !directory.Contains('\0') && !directory.Contains('"') && !directory.Contains('*') && !directory.Contains('?'); }
        catch (ArgumentException) { return false; }
    }
    public ExportLocation? Location(string requirement)
    {
        using var connection = Open(); using var command = connection.CreateCommand();
        command.CommandText = "SELECT Payload FROM ExcelLocation WHERE Requirement=$requirement";
        command.Parameters.AddWithValue("$requirement", requirement.Trim());
        return command.ExecuteScalar() is string json ? JsonSerializer.Deserialize<ExportLocation>(json, Json) : null;
    }
    public ExportLocation WriteExcel(string requirement, string directory, byte[] bytes)
    {
        if (string.IsNullOrWhiteSpace(directory)) directory = ResolveDirectory(directory);
        if (!ValidDirectory(directory)) throw new ArgumentException("Indica una carpeta local absoluta válida (máximo 220 caracteres).");
        directory = Path.GetFullPath(directory.Trim());
        var path = Path.Combine(directory, $"Pruebas Unitarias - {requirement.Trim()}.xlsx");
        lock (gate)
        {
            Directory.CreateDirectory(directory);
            var temporary = Path.Combine(directory, $".evidence-{Guid.NewGuid():N}.tmp");
            try
            {
                File.WriteAllBytes(temporary, bytes);
                // Same-directory rename avoids leaving a partially written workbook.
                File.Move(temporary, path, overwrite: true);
                var location = new ExportLocation(requirement.Trim(), path, DateTimeOffset.UtcNow);
                using var connection = Open(); using var command = connection.CreateCommand();
                command.CommandText = "INSERT INTO ExcelLocation VALUES($requirement,$payload) ON CONFLICT(Requirement) DO UPDATE SET Payload=$payload";
                command.Parameters.AddWithValue("$requirement", location.Requirement);
                command.Parameters.AddWithValue("$payload", JsonSerializer.Serialize(location, Json));
                command.ExecuteNonQuery();
                return location;
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}
public sealed record SaveExcelRequest(EvidenceDocument Document, string? Directory);
