using TerminalDotnet.Testing;

namespace TerminalDotnet.Git;

/// <summary>Git status includes files whose only difference is line endings.
/// Compare the index and working tree separately so staged work remains visible
/// even when the working tree later returns to the committed contents.</summary>
internal static class GitMeaningfulChanges
{
    public static async Task<IReadOnlyList<GitStatusEntry>> FromAsync(
        ICommandRunner runner,
        string repositoryRoot,
        string scope,
        IReadOnlyList<GitStatusEntry> entries,
        CancellationToken cancellationToken)
    {
        if (!entries.Any(NeedsContentCheck))
        {
            return entries;
        }

        var staged = await DiffPathsAsync(runner, repositoryRoot, scope, staged: true, cancellationToken)
            .ConfigureAwait(false);
        var unstaged = await DiffPathsAsync(runner, repositoryRoot, scope, staged: false, cancellationToken)
            .ConfigureAwait(false);
        if (staged.ExitCode != 0 || unstaged.ExitCode != 0)
        {
            return entries;
        }

        var changed = PathsFrom(staged.StandardOutput, repositoryRoot);
        changed.UnionWith(PathsFrom(unstaged.StandardOutput, repositoryRoot));
        return entries
            .Where(entry => !NeedsContentCheck(entry) || changed.Contains(Path.GetFullPath(entry.RelativePath, repositoryRoot)))
            .ToArray();
    }

    private static bool NeedsContentCheck(GitStatusEntry entry) =>
        entry.Kind == GitChangeKind.Modified && !entry.PathChanged;

    private static Task<CommandResult> DiffPathsAsync(
        ICommandRunner runner,
        string repositoryRoot,
        string scope,
        bool staged,
        CancellationToken cancellationToken) =>
        runner.RunAsync(GitRequest.For(
            ["diff", "--no-ext-diff", "--no-textconv", "--ignore-cr-at-eol", "--name-only", "-z",
                .. (staged ? ["--cached"] : Array.Empty<string>()), "--", scope],
            repositoryRoot), cancellationToken);

    private static HashSet<string> PathsFrom(string output, string repositoryRoot) => output
        .Split('\0', StringSplitOptions.RemoveEmptyEntries)
        .Select(path => Path.GetFullPath(path, repositoryRoot))
        .ToHashSet(StringComparer.Ordinal);
}
