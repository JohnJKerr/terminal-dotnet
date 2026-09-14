namespace TerminalDotnet.Files;

/// <summary>
/// A watched working tree reports far more than the panels ever show. A build
/// writes thousands of files under bin and obj, git rewrites its own index on
/// every command, and an editor saves through a scratch copy beside the file it
/// is writing. Reloading for any of that would leave the panels churning against
/// work nobody asked about, so only the paths a reader could see count as edits.
/// </summary>
public static class WorkspaceEdits
{
    private static readonly IReadOnlyList<string> UnwatchedFolders =
        ["bin", "obj", ".git", ".vs", ".idea", "node_modules", "TestResults"];

    /// <summary>What an editor names the copy it writes before the real save.
    /// Each of those lands as an edit of its own moments before the file the
    /// reader actually changed.</summary>
    private static readonly IReadOnlyList<string> ScratchSuffixes =
        ["~", ".swp", ".swo", ".swx", ".tmp"];

    public static bool Matters(string path) =>
        !Segments(path).Any(UnwatchedFolders.Contains) && !IsScratch(path);

    private static IEnumerable<string> Segments(string path) =>
        path.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);

    private static bool IsScratch(string path) => ScratchSuffixes.Any(
        suffix => path.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
}
