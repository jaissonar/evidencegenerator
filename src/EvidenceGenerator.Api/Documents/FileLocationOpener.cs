using System.Diagnostics;

namespace EvidenceGenerator.Api.Documents;

public interface IFileLocationOpener { void Open(string path); }
public sealed class FileLocationOpener : IFileLocationOpener
{
    public void Open(string path)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        var start = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe")) { UseShellExecute = false };
        start.ArgumentList.Add("/select,");
        start.ArgumentList.Add(path);
        using var process = Process.Start(start);
    }
}
public sealed record OpenExcelLocationRequest(string Requirement);
