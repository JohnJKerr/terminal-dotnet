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

        return gitStatuses.EntriesFor(launchFolder, launchFolder, paths);
    }
}
