using System.Runtime.InteropServices;

namespace EvidenceGenerator.Api.Documents;

public static class SystemFolders
{
    // Known Folder lookup also respects a Downloads folder moved to another drive.
    public static string Downloads()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("El guardado local requiere Windows.");
        var initialized = CoInitializeEx(IntPtr.Zero, 0);
        var folder = new Guid("374DE290-123F-4565-9164-39C4925E467B");
        IntPtr path = IntPtr.Zero;
        try
        {
            Marshal.ThrowExceptionForHR(SHGetKnownFolderPath(ref folder, 0x4000, IntPtr.Zero, out path));
            return Marshal.PtrToStringUni(path) ?? throw new IOException("No se pudo localizar la carpeta Descargas del usuario.");
        }
        finally
        {
            if (path != IntPtr.Zero) Marshal.FreeCoTaskMem(path);
            if (initialized >= 0) CoUninitialize();
        }
    }
    [DllImport("shell32.dll", ExactSpelling = true)]
    private static extern int SHGetKnownFolderPath(ref Guid folder, uint flags, IntPtr token, out IntPtr path);
    [DllImport("ole32.dll", ExactSpelling = true)]
    private static extern int CoInitializeEx(IntPtr reserved, uint flags);
    [DllImport("ole32.dll", ExactSpelling = true)]
    private static extern void CoUninitialize();
}
