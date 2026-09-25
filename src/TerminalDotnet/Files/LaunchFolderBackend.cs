using TerminalDotnet.Testing;

namespace TerminalDotnet.Files;

/// <summary>
/// Every file in the folder the app was launched in, whether or not a .NET
/// project claims it, so scripts, docs and workflows are as reachable as the
/// source. Launching further down the tree narrows the panel to that folder.
/// </summary>
public sealed class LaunchFolderBackend(ICommandRunner commandRunner) : IFileExplorerBackend
{
    private readonly GitFileListing listing = new(commandRunner);

    public async Task<IReadOnlyList<FileEntry>> DiscoverAsync(
        string target,
        CancellationToken cancellationToken = default)
    {
        var launchFolder = Path.GetDirectoryName(Path.GetFullPath(target))!;
        var root = await listing.RootAsync(launchFolder, cancellationToken).ConfigureAwait(false);
        var gitStatuses = await listing.StatusesAsync(root, cancellationToken).ConfigureAwait(false);
        var paths = await listing.FilesUnderAsync(launchFolder, cancellationToken).ConfigureAwait(false);

        return
        [
            .. paths.Select(path => FileEntryFor(launchFolder, path, gitStatuses)),
            .. DeletedEntries(launchFolder, gitStatuses)
        ];
    }

    private static FileEntry FileEntryFor(
        string launchFolder,
        string path,
        IReadOnlyDictionary<string, FileGitStatus> gitStatuses) => new(
        launchFolder,
        path,
        gitStatuses.GetValueOrDefault(Path.GetFullPath(path), FileGitStatus.Unchanged));

    /// <summary>A file git still knows about but the disk no longer holds is
    /// counted, so the summary reports the deletion the listing cannot show.</summary>
    private static IEnumerable<FileEntry> DeletedEntries(
        string launchFolder,
        IReadOnlyDictionary<string, FileGitStatus> gitStatuses) => gitStatuses
        .Where(status => status.Value == FileGitStatus.Deleted)
        .Select(status => status.Key)
        .Where(path => GitFileListing.IsUnder(path, launchFolder))
        .OrderBy(path => path, StringComparer.Ordinal)
        .Select(path => new FileEntry(launchFolder, path, FileGitStatus.Deleted));
}
