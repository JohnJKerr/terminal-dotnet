using Xunit;

namespace TerminalDotnet.Tests;

/// <summary>
/// A test of behaviour only POSIX file systems can be asked for, such as a
/// name holding a newline. Windows rejects those names outright, so the test
/// is reported as skipped there rather than quietly passing.
/// </summary>
public sealed class PosixFactAttribute : FactAttribute
{
    public PosixFactAttribute()
    {
        if (OperatingSystem.IsWindows())
        {
            Skip = "Windows file names cannot hold this.";
        }
    }
}
