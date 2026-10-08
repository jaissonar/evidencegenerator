using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using EvidenceGenerator.Api.Documents;

namespace EvidenceGenerator.Tests;

public class ApiTests
{
    [Fact]
    public async Task ApiPersistsDocumentsRejectsConflictsAndBlocksForeignOrigins()
    {
        var directory = Path.Combine(Path.GetTempPath(), "evidence-api-test-" + Guid.NewGuid());
        try
        {
            await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseSetting("DataDirectory", directory));
            using var client = factory.CreateClient();
            var storage = await client.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/storage");
            Assert.Equal(Path.Combine(directory, "evidence.db"), storage.GetProperty("databasePath").GetString());
            var doc = DocumentTests.Sample(1);
            var saved = await client.PutAsJsonAsync($"/api/documents/{doc.Id}", doc);
            Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
            var loaded = await client.GetFromJsonAsync<EvidenceDocument>($"/api/documents/{doc.Id}");
            Assert.Equal(1, loaded!.Revision);
            Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/documents/{doc.Id}", doc)).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/documents/{Guid.NewGuid()}", doc)).StatusCode);
            var excel = await client.PostAsJsonAsync("/api/exports/excel", doc);
            Assert.Equal(HttpStatusCode.OK, excel.StatusCode);
            Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excel.Content.Headers.ContentType!.MediaType);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.DeleteAsync($"/api/documents/{doc.Id}?revision=0")).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/documents/{doc.Id}?revision=2")).StatusCode);
            Assert.NotNull(await client.GetFromJsonAsync<EvidenceDocument>($"/api/documents/{doc.Id}"));
            client.DefaultRequestHeaders.Add("Origin", "https://untrusted.example");
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/documents")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync($"/api/documents/{doc.Id}?revision=1")).StatusCode);
            client.DefaultRequestHeaders.Remove("Origin");
            Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/documents/{doc.Id}?revision=1")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/documents/{doc.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/documents/{doc.Id}?revision=1")).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/documents/{doc.Id}", loaded)).StatusCode);
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
