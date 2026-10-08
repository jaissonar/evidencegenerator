using System.Text.Json;
using EvidenceGenerator.Api.Documents;
namespace EvidenceGenerator.Tests;
public class SearchTests
{
    [Fact]
    public void FiltersAllDraftsByAccentInsensitiveTextAndInclusiveDocumentDates()
    {
        var directory = Path.Combine(Path.GetTempPath(), "search-test-" + Guid.NewGuid());
        try {
            var store = new DocumentStore(directory);
            var target = DocumentTests.Sample(0) with { Requirement = "MD 999", Client = "Compañía Única", Description = "Descripción especial", Date = new DateOnly(2026, 1, 15), Mail = DocumentTests.Sample().Mail with { BodyHtml = "<p><strong>Correo</strong></p>" } };
            store.Save(target);
            for (var i = 0; i < 101; i++) store.Save(DocumentTests.Sample(0));
            Assert.Equal(100, JsonSerializer.SerializeToElement(store.List()).GetArrayLength());
            Assert.Equal(2, JsonSerializer.SerializeToElement(store.List(offset:100)).GetArrayLength());
            foreach (var query in new[] { "md 999", "compañia unica", "DESCRIPCION especial" }) {
                var result = JsonSerializer.SerializeToElement(store.List(query, new(2026,1,15), new(2026,1,15)));
                Assert.Equal(1,result.GetArrayLength()); Assert.Equal(target.Id.ToString(),result[0].GetProperty("id").GetString());
            }
            Assert.Equal(0,JsonSerializer.SerializeToElement(store.List("MD 999",new(2026,1,16))).GetArrayLength());
            Assert.Equal(0,JsonSerializer.SerializeToElement(store.List("%' OR 1=1 --")).GetArrayLength());
            Assert.Equal(target.Mail.BodyHtml,store.Get(target.Id)!.Mail.BodyHtml);
            Assert.NotNull(DocumentValidation.Validate(target with { Mail = target.Mail with { BodyHtml = new string('a',100001) } }));
        } finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); Directory.Delete(directory,true); }
    }
}

