using TerminalDotnet.Git;
using TerminalDotnet.Testing;

namespace TerminalDotnet.Files;

/// <summary>The git plumbing the file backends share: where the repository
/// starts, which files it holds, and what has changed inside it.</summary>
internal sealed class GitFileListing(ICommandRunner commandRunner)
{
    private static readonly IReadOnlyDictionary<string, FileGitStatus> Unchanged =
        new Dictionary<string, FileGitStatus>();

    public async Task<string?> RootAsync(
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        var result = await commandRunner.RunAsync(
            GitRequest.For(["rev-parse", "--show-toplevel"], workingDirectory),
            cancellationToken);

        return result.ExitCode == 0 ? result.StandardOutput.Trim() : null;
    }

    /// <summary>Nothing counts as changed outside a repository, which is why
    /// the root is allowed to be missing.</summary>
    public async Task<IReadOnlyDictionary<string, FileGitStatus>> StatusesAsync(
        string? repositoryRoot,
        CancellationToken cancellationToken)
    {
        if (repositoryRoot is null)
        {
            return Unchanged;
        }

        var result = await commandRunner.RunAsync(
            GitRequest.For(["status", "--porcelain=v1", "-z", "--untracked-files=all"], repositoryRoot),
            cancellationToken);

        return result.ExitCode == 0
            ? StatusesFrom(result.StandardOutput, repositoryRoot)
            : Unchanged;
    }

    /// <summary>Falls back to the files on disk when git cannot answer, so a
    /// directory outside a repository still fills the panel.</summary>
    public async Task<IReadOnlyList<string>> FilesUnderAsync(
        string directory,
        CancellationToken cancellationToken)
    {
        var listing = await commandRunner.RunAsync(
            GitRequest.For(["ls-files", "-z", "--cached", "--others", "--exclude-standard"], directory),
            cancellationToken);

        return listing.ExitCode == 0
            ? TrackedFiles(listing.StandardOutput, directory)
            : FilesOnDisk(directory);
    }

    /// <summary>Whether the folder holds the file, so a listing scoped to one
    /// folder does not pick up a change from somewhere else in the repository.</summary>
    public static bool IsUnder(string path, string folder) =>
        path.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.Ordinal) &&
        IsSourceFile(path);

    /// <summary>Build output belongs to the compiler rather than the reader, so
    /// it stays out of every listing.</summary>
    public static bool IsSourceFile(string path)
    {
        var segments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return !segments.Contains("bin", StringComparer.OrdinalIgnoreCase) &&
            !segments.Contains("obj", StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyDictionary<string, FileGitStatus> StatusesFrom(
        string output,
        string repositoryRoot) => GitStatusOutput.EntriesFrom(output)
        .ToDictionary(
            entry => Path.GetFullPath(entry.RelativePath, repositoryRoot),
            entry => StatusFrom(entry.Kind),
            StringComparer.Ordinal);

    private static FileGitStatus StatusFrom(GitChangeKind kind) => kind switch
    {
        GitChangeKind.Added => FileGitStatus.New,
        GitChangeKind.Deleted => FileGitStatus.Deleted,
        _ => FileGitStatus.Modified
    };

    private static IReadOnlyList<string> TrackedFiles(string listing, string directory) => listing
        .Split('\0', StringSplitOptions.RemoveEmptyEntries)
        .Select(path => Path.GetFullPath(path, directory))
        .Where(File.Exists)
        .Where(IsSourceFile)
        .OrderBy(path => path, StringComparer.Ordinal)
        .ToArray();

    private static IReadOnlyList<string> FilesOnDisk(string directory) => SourceTree
        .FilesUnder(directory, "*")
        .OrderBy(path => path, StringComparer.Ordinal)
        .ToArray();
}
