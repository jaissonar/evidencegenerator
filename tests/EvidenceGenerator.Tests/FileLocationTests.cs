using System.Net;
using System.Net.Http.Json;
using EvidenceGenerator.Api.Documents;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace EvidenceGenerator.Tests;
public class FileLocationTests
{
    private sealed class FakeOpener : IFileLocationOpener
    {
        public string? Path { get; private set; }
        public void Open(string path) => Path = path;
    }
    [Fact]
    public async Task OpensOnlyRegisteredExistingExcelAndRejectsForeignOrigins()
    {
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "evidence-open-test-" + Guid.NewGuid());
        var opener = new FakeOpener();
        try
        {
            await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
                builder.UseSetting("DataDirectory", directory).ConfigureServices(services => services.AddSingleton<IFileLocationOpener>(opener)));
            using var client = factory.CreateClient();
            var doc = DocumentTests.Sample();
            Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/exports/open-location", new { requirement = doc.Requirement })).StatusCode);
            Assert.Null(opener.Path);
            var response = await client.PostAsJsonAsync("/api/exports/save", new SaveExcelRequest(doc, directory));
            var location = (await response.Content.ReadFromJsonAsync<ExportLocation>())!;
            Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/exports/open-location", new { requirement = doc.Requirement, path = @"C:\unrelated.txt" })).StatusCode);
            Assert.Equal(location.Path, opener.Path);
            client.DefaultRequestHeaders.Add("Origin", "https://example.org");
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/exports/open-location", new { requirement = doc.Requirement })).StatusCode);
            client.DefaultRequestHeaders.Remove("Origin");
            File.Delete(location.Path);
            Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/exports/open-location", new { requirement = doc.Requirement })).StatusCode);
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
