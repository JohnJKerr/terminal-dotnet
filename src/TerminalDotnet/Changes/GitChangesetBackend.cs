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
            ["status", "--porcelain=v1", "--untracked-files=all", "--", scopeDirectory],
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

        var tracked = await GitAsync(["diff", "HEAD", "--", file.Path], cancellationToken);
        if (tracked.StandardOutput.Length > 0)
        {
            return tracked.StandardOutput;
        }

        var untracked = await GitAsync(
            ["diff", "--no-index", "--", "/dev/null", file.Path],
            cancellationToken);
        return untracked.StandardOutput;
    }

    public async Task RestoreAsync(ChangedFile file, CancellationToken cancellationToken = default)
    {
        if (repositoryRoot is null)
        {
            return;
        }

        await GitAsync(["restore", "--staged", "--worktree", "--", file.Path], cancellationToken);
    }

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
            KindFrom(entry.Kind));
    }

    private static ChangeKind KindFrom(GitChangeKind kind) => kind switch
    {
        GitChangeKind.Added => ChangeKind.Added,
        GitChangeKind.Deleted => ChangeKind.Deleted,
        _ => ChangeKind.Modified
    };
}
