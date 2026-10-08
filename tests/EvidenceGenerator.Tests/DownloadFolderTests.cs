using EvidenceGenerator.Api.Documents;

namespace EvidenceGenerator.Tests;

public class DownloadFolderTests
{
    [Fact]
    public void EmptyPathsUseSystemDefaultWhileCustomPathsRemainUnchanged()
    {
        var root = Path.Combine(Path.GetTempPath(), "evidence-downloads-test-" + Guid.NewGuid());
        var downloads = Path.Combine(root, "Redirected downloads");
        try
        {
            var store = new WorkspaceStore(new DocumentStore(root), () => downloads);
            Assert.Equal(downloads, store.Get().ExcelDirectory);
            var settings = store.Get() with { Name = "QA", ExcelDirectory = "   " };
            Assert.Null(WorkspaceStore.Validate(settings));
            Assert.Equal(downloads, store.Save(settings)!.ExcelDirectory);
            Assert.Equal(downloads, store.Get().ExcelDirectory);
            var saved = store.WriteExcel("MD DEFAULT", "  ", [1, 2, 3]);
            Assert.Equal(Path.Combine(downloads, "Pruebas Unitarias - MD DEFAULT.xlsx"), saved.Path);
            Assert.Equal(new byte[] { 1, 2, 3 }, File.ReadAllBytes(saved.Path));
            var custom = Path.Combine(root, "Custom");
            Assert.Equal(custom, store.Save(store.Get() with { ExcelDirectory = custom })!.ExcelDirectory);
            Assert.Equal(custom, store.Get().ExcelDirectory);
            Assert.NotNull(WorkspaceStore.Validate(store.Get() with { ExcelDirectory = "relative-folder" }));
            Assert.Throws<ArgumentException>(() => store.WriteExcel("MD DEFAULT", "relative-folder", [1]));
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void WindowsDownloadsLookupReturnsAnAbsolutePathWithoutCreatingFiles()
    {
        if (OperatingSystem.IsWindows()) Assert.True(Path.IsPathFullyQualified(SystemFolders.Downloads()));
    }
}
