using Xunit;

namespace TerminalDotnet.Tests;

/// <summary>
/// A test of behaviour only a POSIX system can be asked for, such as a file
/// name holding a newline or a shell script's process tree. It is reported as
/// skipped on Windows rather than quietly passing.
/// </summary>
public sealed class PosixFactAttribute : FactAttribute
{
    public PosixFactAttribute(string windowsReason = "Windows file names cannot hold this.")
    {
        if (OperatingSystem.IsWindows())
        {
            Skip = windowsReason;
        }
    }
}
