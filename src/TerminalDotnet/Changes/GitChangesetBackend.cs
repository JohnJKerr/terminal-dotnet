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

        return GitStatusOutput.EntriesFrom(status.StandardOutput)
            .Select(entry => ChangedFileFrom(entry, repositoryRoot, scopeDirectory))
            .OrderBy(file => file.DisplayPath, StringComparer.Ordinal)
            .ToArray();
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

    private static IReadOnlyList<string> RestoreArgumentsFor(ChangedFile file) =>
        file.Unstaged == ChangeKind.Deleted
            ? RestoreFromIndex(file)
            : RestoreFromLastCommit(file);

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
