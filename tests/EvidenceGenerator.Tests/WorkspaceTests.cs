using System.Net;
using System.Net.Http.Json;
using EvidenceGenerator.Api.Documents;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EvidenceGenerator.Tests;

public class WorkspaceTests
{
    [Fact]
    public async Task PreferencesPersistAndRejectConflictsInvalidContactsAndForeignOrigins()
    {
        var directory = Path.Combine(Path.GetTempPath(), "evidence-settings-" + Guid.NewGuid());
        try
        {
            await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseSetting("DataDirectory", directory));
            using var client = factory.CreateClient();
            var settings = (await client.GetFromJsonAsync<WorkspaceSettings>("/api/settings"))!;
            settings = settings with { Name = "Responsable QA", ExcelDirectory = Path.Combine(directory, "exports"),
                Contacts = [new("one", "Ana", "ana@example.com")], EnvironmentUrls = ["https://example.com"], Connections = ["Test", "Oracle Test"] };
            Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync("/api/settings", settings)).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync("/api/settings", settings)).StatusCode);
            var loaded = (await client.GetFromJsonAsync<WorkspaceSettings>("/api/settings"))!;
            Assert.Equal("Responsable QA", loaded.Name); Assert.Equal("ana@example.com", loaded.Contacts[0].Email);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/settings", loaded with { Contacts = [new("one", "Ana", "invalid")] })).StatusCode);
            Assert.Equal(new[] { "Test", "Oracle Test" }, loaded.Connections);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/settings", loaded with { EnvironmentUrls = ["javascript:alert(1)"] })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/settings", loaded with { Connections = ["Test", "test"] })).StatusCode);
            client.DefaultRequestHeaders.Add("Origin", "https://example.com");
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync("/api/settings", loaded)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/exports/save", new SaveExcelRequest(DocumentTests.Sample(), directory))).StatusCode);
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public async Task ExportUpdatesSameWorkbookAndKeepsPreviousFileWhenLocked()
    {
        var directory = Path.Combine(Path.GetTempPath(), "evidence-export-" + Guid.NewGuid());
        try
        {
            await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseSetting("DataDirectory", directory));
            using var client = factory.CreateClient();
            var doc = DocumentTests.Sample();
            var folder = Path.Combine(directory, "exports");
            var first = await client.PostAsJsonAsync("/api/exports/save", new SaveExcelRequest(doc, folder));
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            var location = (await first.Content.ReadFromJsonAsync<ExportLocation>())!;
            var initial = File.ReadAllBytes(location.Path);
            doc = doc with { Description = "Contenido actualizado" };
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/exports/save", new SaveExcelRequest(doc, folder))).StatusCode);
            Assert.Single(Directory.GetFiles(folder));
            Assert.False(initial.SequenceEqual(File.ReadAllBytes(location.Path)));
            var updated = File.ReadAllBytes(location.Path);
            using (var fileLock = new FileStream(location.Path, FileMode.Open, FileAccess.Read, FileShare.None))
                Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/exports/save", new SaveExcelRequest(doc, folder))).StatusCode);
            Assert.Equal(updated, File.ReadAllBytes(location.Path));
            Assert.Single(Directory.GetFiles(folder));
            var saved = await client.GetFromJsonAsync<ExportLocation>("/api/exports/location?requirement=MD%2012345");
            Assert.Equal(location.Path, saved!.Path);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/exports/save", new SaveExcelRequest(doc, "relative-folder"))).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/exports/save", new SaveExcelRequest(doc with { Requirement = "../outside" }, folder))).StatusCode);
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
