using EvidenceGenerator.Api.Documents;
namespace EvidenceGenerator.Tests;
public class CatalogTests
{
    [Fact]
    public void LegacyCatalogSplitsWithoutLosingValuesOrRestoringDeletedEntries()
    {
        var legacy = new WorkspaceSettings(3, "QA", null, @"C:\Exports", "ask", null, 420, [], [
            new("1", "One", "SQL", "https://example.com", "Test"),
            new("2", "Two", "Oracle", "https://example.com", "Oracle Test")]);
        var migrated = WorkspaceStore.Normalize(legacy);
        Assert.Equal(new[] { "https://example.com" }, migrated.EnvironmentUrls);
        Assert.Equal(new[] { "Test", "Oracle Test" }, migrated.Connections);
        Assert.Equal(3, migrated.Revision);
        Assert.Null(migrated.Environments);
        var cleared = WorkspaceStore.Normalize(legacy with { EnvironmentUrls = [], Connections = [] });
        Assert.Empty(cleared.EnvironmentUrls!); Assert.Empty(cleared.Connections!);
    }
}
