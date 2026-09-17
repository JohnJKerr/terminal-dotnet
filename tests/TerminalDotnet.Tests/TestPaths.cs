namespace TerminalDotnet.Tests;

/// <summary>
/// Paths for a repository that is never written to disk. The app resolves and
/// compares paths through the file system's own rules, and a bare "/repo" is
/// not a fully qualified path on Windows, so the tests name the same imaginary
/// repository the way the platform running them would.
/// </summary>
internal static class TestPaths
{
    /// <summary>`/repo` on Linux and macOS, `C:\repo` on Windows.</summary>
    public static string Repo { get; } = Path.GetFullPath(Path.Combine("/", "repo"));

    public static string In(params string[] parts) => Path.Combine(Repo, Path.Combine(parts));
}
