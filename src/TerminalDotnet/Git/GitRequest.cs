using TerminalDotnet.Testing;

namespace TerminalDotnet.Git;

/// <summary>
/// A git command run inside the repository being read. Git takes settings from
/// that repository's own configuration, and some of them name programs to run:
/// `core.fsmonitor` runs on every status. A clone cannot carry its
/// configuration, but a repository unpacked from an archive or shared from
/// another account can, so the monitor is switched off on every call.
/// </summary>
public static class GitRequest
{
    public static CommandRequest For(IReadOnlyList<string> arguments, string workingDirectory) =>
        new("git", ["-c", "core.fsmonitor=false", .. arguments], workingDirectory);
}
