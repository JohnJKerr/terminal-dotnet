using TerminalDotnet.Git;
using TerminalDotnet.Testing;

namespace TerminalDotnet.Changes;

/// <summary>
/// The changeset as git reports it. Each file is named by its full path, so
/// git is asked about it from wherever the file is rather than from wherever
/// the changeset was last discovered.
/// </summary>
public sealed class GitChangesetBackend(ICommandRunner commandRunner) : IChangesetBackend
{
    public async Task<IReadOnlyList<ChangedFile>> DiscoverAsync(
        string target,
        CancellationToken cancellationToken = default)
    {
        var scopeDirectory = Path.GetDirectoryName(Path.GetFullPath(target))!;
        var repositoryRoot = await GitRepository.RootAsync(commandRunner, scopeDirectory, cancellationToken)
            .ConfigureAwait(false);
        if (repositoryRoot is null)
        {
            return [];
        }

        var status = await GitAsync(
            repositoryRoot,
            ["status", "--porcelain=v1", "-z", "--untracked-files=all", "--", scopeDirectory],
            cancellationToken).ConfigureAwait(false);
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
        var folder = NearestFolderOnDisk(file);
        var tracked = await GitAsync(
            folder,
            ["diff", .. WithoutConfiguredTools, "HEAD", "--", Pathspec(file)],
            cancellationToken).ConfigureAwait(false);
        if (tracked.StandardOutput.Length > 0)
        {
            return tracked.StandardOutput;
        }

        var untracked = await GitAsync(
            folder,
            ["diff", .. WithoutConfiguredTools, "--no-index", "--", "/dev/null", file.Path],
            cancellationToken).ConfigureAwait(false);
        return untracked.StandardOutput;
    }

    /// <summary>A repository's configuration can hand diffs to an external
    /// program or convert files through a textconv driver before comparing
    /// them; the panel shows git's own diff instead.</summary>
    private static readonly IReadOnlyList<string> WithoutConfiguredTools = ["--no-ext-diff", "--no-textconv"];

    public async Task<bool> RestoreAsync(
        ChangedFile file,
        CancellationToken cancellationToken = default)
    {
        var restore = await GitAsync(NearestFolderOnDisk(file), RestoreArgumentsFor(file), cancellationToken)
            .ConfigureAwait(false);
        return restore.ExitCode == 0;
    }

    /// <summary>A deleted file can take its folder with it, so git is run
    /// from the closest folder above it that is still there.</summary>
    private static string NearestFolderOnDisk(ChangedFile file)
    {
        var folder = Path.GetDirectoryName(file.Path);
        while (folder is not null && !Directory.Exists(folder))
        {
            folder = Path.GetDirectoryName(folder);
        }

        return folder ?? Path.GetPathRoot(file.Path)!;
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

    private Task<CommandResult> GitAsync(
        string workingDirectory,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken) =>
        commandRunner.RunAsync(GitRequest.For(arguments, workingDirectory), cancellationToken);

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
