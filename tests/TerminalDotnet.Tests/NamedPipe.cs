using System.Runtime.InteropServices;

namespace TerminalDotnet.Tests;

/// <summary>
/// A named pipe on disk. Opening one to read waits until something opens it
/// to write, so a reader that simply opens every file it is shown hangs on it.
/// Windows keeps its named pipes off the file system, so none is made there.
/// </summary>
internal static class NamedPipe
{
    private const uint OwnerReadWrite = 0b110_000_000;

    public static bool TryCreate(string path) =>
        !OperatingSystem.IsWindows() && mkfifo(path, OwnerReadWrite) == 0;

    [DllImport("libc", SetLastError = true)]
    private static extern int mkfifo(string path, uint mode);
}
