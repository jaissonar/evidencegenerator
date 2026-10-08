using Microsoft.Data.Sqlite;
using System.Text.Json;

namespace EvidenceGenerator.Api.Documents;

public sealed class DocumentStore
{
    private readonly string connectionString;
    public string DatabasePath { get; }
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public DocumentStore(string directory)
    {
        directory = Path.GetFullPath(directory);
        Directory.CreateDirectory(directory);
        DatabasePath = Path.Combine(directory, "evidence.db");
        connectionString = new SqliteConnectionStringBuilder { DataSource = DatabasePath }.ToString();
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            PRAGMA journal_mode=WAL;
            CREATE TABLE IF NOT EXISTS Document (
                Id TEXT PRIMARY KEY, Requirement TEXT NOT NULL, Revision INTEGER NOT NULL,
                UpdatedAt TEXT NOT NULL, Payload TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_Document_UpdatedAt ON Document(UpdatedAt);
            PRAGMA user_version=1;
            """;
        command.ExecuteNonQuery();
    }
    private SqliteConnection Open() { var connection = new SqliteConnection(connectionString); connection.Open(); return connection; }
    public object List(string? search = null, DateOnly? from = null, DateOnly? to = null, int offset = 0)
    {
        using var connection = Open(); using var command = connection.CreateCommand();
        connection.CreateFunction<string, string, bool>("contains_text", (value, query) =>
            System.Globalization.CultureInfo.GetCultureInfo("es-CO").CompareInfo.IndexOf(value ?? "", query,
                System.Globalization.CompareOptions.IgnoreCase | System.Globalization.CompareOptions.IgnoreNonSpace) >= 0);
        command.CommandText = """
            SELECT Id, Requirement, Revision, UpdatedAt,
                COALESCE(json_extract(Payload,'$.client'),''),
                COALESCE(json_extract(Payload,'$.description'),''), json_extract(Payload,'$.date')
            FROM Document
            WHERE ($search='' OR contains_text(Requirement,$search)
                OR contains_text(json_extract(Payload,'$.client'),$search)
                OR contains_text(json_extract(Payload,'$.description'),$search))
              AND ($from='' OR json_extract(Payload,'$.date') >= $from)
              AND ($to='' OR json_extract(Payload,'$.date') <= $to)
            ORDER BY UpdatedAt DESC, Id LIMIT 100 OFFSET $offset
            """;
        command.Parameters.AddWithValue("$search", search?.Trim() ?? "");
        command.Parameters.AddWithValue("$from", from?.ToString("yyyy-MM-dd") ?? "");
        command.Parameters.AddWithValue("$to", to?.ToString("yyyy-MM-dd") ?? "");
        command.Parameters.AddWithValue("$offset", Math.Max(0, offset));
        using var reader = command.ExecuteReader(); var rows = new List<object>();
        while (reader.Read()) rows.Add(new { id = reader.GetString(0), requirement = reader.GetString(1), revision = reader.GetInt32(2), updatedAt = reader.GetString(3), client = reader.GetString(4), description = reader.GetString(5), date = reader.GetString(6) });
        return rows;
    }
    public EvidenceDocument? Get(Guid id)
    {
        using var connection = Open(); using var command = connection.CreateCommand();
        command.CommandText = "SELECT Payload FROM Document WHERE Id=$id"; command.Parameters.AddWithValue("$id", id.ToString());
        return command.ExecuteScalar() is string json ? JsonSerializer.Deserialize<EvidenceDocument>(json, Json) : null;
    }
    public bool Delete(Guid id, int revision)
    {
        using var connection = Open(); using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Document WHERE Id=$id AND Revision=$revision";
        command.Parameters.AddWithValue("$id", id.ToString());
        command.Parameters.AddWithValue("$revision", revision);
        return command.ExecuteNonQuery() == 1;
    }

    public EvidenceDocument? Save(EvidenceDocument doc)
    {
        var saved = doc with { Revision = doc.Revision + 1 };
        using var connection = Open(); using var command = connection.CreateCommand();
        command.CommandText = doc.Revision == 0
            ? "INSERT OR IGNORE INTO Document VALUES ($id,$requirement,$revision,$updated,$payload)"
            : "UPDATE Document SET Requirement=$requirement,Revision=$revision,UpdatedAt=$updated,Payload=$payload WHERE Id=$id AND Revision=$previous";
        command.Parameters.AddWithValue("$id", doc.Id.ToString()); command.Parameters.AddWithValue("$requirement", doc.Requirement);
        command.Parameters.AddWithValue("$revision", saved.Revision); command.Parameters.AddWithValue("$previous", doc.Revision);
        command.Parameters.AddWithValue("$updated", DateTimeOffset.UtcNow.ToString("O"));
        command.Parameters.AddWithValue("$payload", JsonSerializer.Serialize(saved, Json));
        return command.ExecuteNonQuery() == 1 ? saved : null;
    }
}
