namespace TerminalDotnet.Files;

internal sealed record FileTreeNode(string Key, VisibleFileNode Node);

/// <summary>
/// Lays the discovered files out as folder nodes and file nodes. The root each
/// file hangs from is its <see cref="FileEntry.ProjectPath"/>, which the
/// grouping reads either as the project that claims the file or as the folder
/// that holds it.
/// </summary>
internal static class FileTree
{
    public static IReadOnlyList<FileTreeNode> Of(
        IReadOnlyList<FileEntry> files,
        FileGrouping grouping) => files
        .Where(file => file.GitStatus != FileGitStatus.Deleted)
        .GroupBy(file => file.ProjectPath, StringComparer.Ordinal)
        .OrderBy(root => root.Key, StringComparer.Ordinal)
        .SelectMany(root => RootNodes(root, grouping))
        .ToArray();

    private static IEnumerable<FileTreeNode> RootNodes(
        IGrouping<string, FileEntry> root,
        FileGrouping grouping) => grouping == FileGrouping.Project
        ? ProjectNodes(root)
        : ChildNodes(PlacementsUnder(DirectoryOf(root.Key), root), root.Key, 0);

    /// <summary>A project heads a node of its own so that two projects holding
    /// the same folder name stay apart.</summary>
    private static IEnumerable<FileTreeNode> ProjectNodes(IGrouping<string, FileEntry> project)
    {
        var placements = PlacementsUnder(DirectoryOf(Path.GetDirectoryName(project.Key)), project);
        var projectNode = new FileTreeNode(
            project.Key,
            new VisibleFileNode(
                0,
                FileNodeKind.Project,
                Path.GetFileNameWithoutExtension(project.Key),
                Snapshot.Of(placements.Select(placement => placement.File))));

        return [projectNode, .. ChildNodes(placements, project.Key, 1)];
    }

    private static string DirectoryOf(string? path) =>
        string.IsNullOrEmpty(path) ? "." : path;

    private static IReadOnlyList<FilePlacement> PlacementsUnder(
        string rootDirectory,
        IEnumerable<FileEntry> files) =>
        files.Select(file => PlacementFor(rootDirectory, file)).ToArray();

    private static FilePlacement PlacementFor(string rootDirectory, FileEntry file)
    {
        var relativePath = Path.GetRelativePath(rootDirectory, file.Path);
        var segments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return new FilePlacement(segments[..^1], file);
    }

    private static IEnumerable<FileTreeNode> ChildNodes(
        IReadOnlyList<FilePlacement> placements,
        string parentKey,
        int depth)
    {
        var folders = placements
            .Where(placement => placement.Folders.Count > 0)
            .GroupBy(placement => placement.Folders[0], StringComparer.Ordinal)
            .OrderBy(folder => folder.Key, StringComparer.Ordinal)
            .SelectMany(folder => FolderNodes(folder, parentKey, depth));
        var files = placements
            .Where(placement => placement.Folders.Count == 0)
            .OrderBy(placement => placement.File.Path, StringComparer.Ordinal)
            .Select(placement => FileNode(placement.File, parentKey, depth));

        return [.. folders, .. files];
    }

    private static IEnumerable<FileTreeNode> FolderNodes(
        IGrouping<string, FilePlacement> folder,
        string parentKey,
        int depth)
    {
        var key = $"{parentKey}/{folder.Key}";
        var contents = folder.Select(placement => placement.WithoutLeadingFolder()).ToArray();
        var folderNode = new FileTreeNode(
            key,
            new VisibleFileNode(
                depth,
                FileNodeKind.Folder,
                folder.Key,
                Snapshot.Of(contents.Select(placement => placement.File))));

        return [folderNode, .. ChildNodes(contents, key, depth + 1)];
    }

    private static FileTreeNode FileNode(FileEntry file, string parentKey, int depth) => new(
        $"{parentKey}/{Path.GetFileName(file.Path)}",
        new VisibleFileNode(depth, FileNodeKind.File, Path.GetFileName(file.Path), [file]));

    private sealed record FilePlacement(IReadOnlyList<string> Folders, FileEntry File)
    {
        public FilePlacement WithoutLeadingFolder() =>
            this with { Folders = Folders.Skip(1).ToArray() };
    }
}
