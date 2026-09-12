using TerminalDotnet.Git;
using TerminalDotnet.Testing;

namespace TerminalDotnet.Changes;

public sealed class GitChangesetBackend(ICommandRunner commandRunner) : IChangesetBackend
{
    private string? repositoryRoot;
    private string scopeDirectory = "";

    public async Task<IReadOnlyList<ChangedFile>> DiscoverAsync(
        string target,
        CancellationToken cancellationToken = default)
    {
        scopeDirectory = Path.GetDirectoryName(Path.GetFullPath(target))!;
        repositoryRoot = await RepositoryRootAsync(cancellationToken);
        if (repositoryRoot is null)
        {
            return [];
        }

        var status = await GitAsync(
            ["status", "--porcelain=v1", "-z", "--untracked-files=all", "--", scopeDirectory],
            cancellationToken);
        if (status.ExitCode != 0)
        {
            return [];
        }

        return MarkingRecreatedFiles(GitStatusOutput.EntriesFrom(status.StandardOutput)
            .Select(entry => ChangedFileFrom(entry, repositoryRoot, scopeDirectory))
            .OrderBy(file => file.DisplayPath, StringComparer.Ordinal)
            .ToArray());
    }

    public async Task<string> DiffAsync(
        ChangedFile file,
        CancellationToken cancellationToken = default)
    {
        if (repositoryRoot is null)
        {
            return "";
        }

        var tracked = await GitAsync(["diff", "HEAD", "--", Pathspec(file)], cancellationToken);
        if (tracked.StandardOutput.Length > 0)
        {
            return tracked.StandardOutput;
        }

        var untracked = await GitAsync(
            ["diff", "--no-index", "--", "/dev/null", file.Path],
            cancellationToken);
        return untracked.StandardOutput;
    }

    public async Task<bool> RestoreAsync(
        ChangedFile file,
        CancellationToken cancellationToken = default)
    {
        if (repositoryRoot is null)
        {
            return false;
        }

        var restore = await GitAsync(RestoreArgumentsFor(file), cancellationToken);
        return restore.ExitCode == 0;
    }

    /// <summary>Git lists a file deleted in the index and written again in the
    /// working tree as two rows, a staged deletion and an untracked file. The
    /// deletion is marked so restoring it does not overwrite the new contents.
    /// </summary>
    private static IReadOnlyList<ChangedFile> MarkingRecreatedFiles(IReadOnlyList<ChangedFile> files)
    {
        var writtenAgain = files
            .Where(Untracked)
            .Select(file => file.Path)
            .ToHashSet(StringComparer.Ordinal);
        return files
            .Select(file => file with
            {
                Recreated = StagedDeletion(file) && writtenAgain.Contains(file.Path)
            })
            .ToArray();
    }

    private static bool Untracked(ChangedFile file) =>
        file.Staged is null && file.Unstaged == ChangeKind.Added;

    private static bool StagedDeletion(ChangedFile file) =>
        file.Staged == ChangeKind.Deleted && file.Unstaged is null;

    private static IReadOnlyList<string> RestoreArgumentsFor(ChangedFile file) => file switch
    {
        { Recreated: true } => RestoreIndexOnly(file),
        // Ignored files are absent from status, and files can be recreated after discovery.
        { Staged: ChangeKind.Deleted } when Path.Exists(file.Path) => RestoreIndexOnly(file),
        { Unstaged: ChangeKind.Deleted } => RestoreFromIndex(file),
        _ => RestoreFromLastCommit(file)
    };

    private static IReadOnlyList<string> RestoreIndexOnly(ChangedFile file) =>
        ["restore", "--staged", "--", Pathspec(file)];

    private static IReadOnlyList<string> RestoreFromIndex(ChangedFile file) =>
        ["restore", "--worktree", "--", Pathspec(file)];

    private static IReadOnlyList<string> RestoreFromLastCommit(ChangedFile file) =>
        ["restore", "--staged", "--worktree", "--", Pathspec(file)];

    /// <summary>Git reads a path after `--` as a pattern, so a file genuinely
    /// named `*.cs` would otherwise sweep up every sibling it matches. The
    /// literal prefix keeps an operation to the file the panel selected.</summary>
    private static string Pathspec(ChangedFile file) => $":(literal){file.Path}";

    private async Task<string?> RepositoryRootAsync(CancellationToken cancellationToken)
    {
        var result = await commandRunner.RunAsync(
            new CommandRequest("git", ["rev-parse", "--show-toplevel"], scopeDirectory),
            cancellationToken);
        return result.ExitCode == 0 ? result.StandardOutput.Trim() : null;
    }

    private Task<CommandResult> GitAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken) =>
        commandRunner.RunAsync(
            new CommandRequest("git", arguments, repositoryRoot!),
            cancellationToken);

    private static ChangedFile ChangedFileFrom(
        GitStatusEntry entry,
        string repositoryRoot,
        string scopeDirectory)
    {
        var path = Path.GetFullPath(entry.RelativePath, repositoryRoot);
        return new ChangedFile(
            path,
            Path.GetRelativePath(scopeDirectory, path),
            KindFrom(entry.Kind))
        {
            Staged = entry.Staged is { } staged ? KindFrom(staged) : null,
            Unstaged = entry.Unstaged is { } unstaged ? KindFrom(unstaged) : null
        };
    }

    private static ChangeKind KindFrom(GitChangeKind kind) => kind switch
    {
        GitChangeKind.Added => ChangeKind.Added,
        GitChangeKind.Deleted => ChangeKind.Deleted,
        _ => ChangeKind.Modified
    };
}
