namespace TerminalDotnet.Files;

/// <summary>
/// What git says has changed in the repository, by full path. A file git
/// still knows about but the disk no longer holds is listed too, so the
/// summary reports the deletion the listing itself cannot show.
/// </summary>
internal sealed class GitStatuses(IReadOnlyDictionary<string, FileGitStatus> byPath)
{
    public static readonly GitStatuses None = new(new Dictionary<string, FileGitStatus>());

    /// <summary>The entries for the files beneath a folder, each hung from
    /// <paramref name="root"/>, followed by the files deleted from it.</summary>
    public IReadOnlyList<FileEntry> EntriesFor(string root, string folder, IEnumerable<string> paths) =>
    [
        .. paths.Select(path => new FileEntry(root, path, StatusOf(path))),
        .. DeletedUnder(folder).Select(path => new FileEntry(root, path, FileGitStatus.Deleted))
    ];

    private FileGitStatus StatusOf(string path) =>
        byPath.GetValueOrDefault(Path.GetFullPath(path), FileGitStatus.Unchanged);

    private IEnumerable<string> DeletedUnder(string folder) => byPath
        .Where(status => status.Value == FileGitStatus.Deleted)
        .Select(status => status.Key)
        .Where(path => GitFileListing.IsUnder(path, folder))
        .Order(StringComparer.Ordinal);
}
