namespace TerminalDotnet.Files;

public sealed class FileExplorerSession(IFileExplorerBackend backend)
{
    public FileExplorerState State { get; private set; } = new([]);

    public async Task LoadAsync(string target, CancellationToken cancellationToken = default)
    {
        var files = await backend.DiscoverAsync(target, cancellationToken);
        State = new FileExplorerState(NodesFrom(files));
    }

    private static IReadOnlyList<VisibleFileNode> NodesFrom(IReadOnlyList<FileEntry> files) => files
        .GroupBy(file => file.ProjectPath)
        .OrderBy(group => group.Key, StringComparer.Ordinal)
        .SelectMany(ProjectNodes)
        .ToArray();

    private static IEnumerable<VisibleFileNode> ProjectNodes(IGrouping<string, FileEntry> project)
    {
        var projectFiles = project.ToArray();
        yield return new VisibleFileNode(
            0,
            FileNodeKind.Project,
            Path.GetFileNameWithoutExtension(project.Key),
            projectFiles);
        foreach (var node in project
            .GroupBy(file => file.Namespace)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .SelectMany(NamespaceNodes))
        {
            yield return node;
        }
    }

    private static IEnumerable<VisibleFileNode> NamespaceNodes(IGrouping<string, FileEntry> namespaceFiles)
    {
        var files = namespaceFiles.OrderBy(file => file.Path, StringComparer.Ordinal).ToArray();
        yield return new VisibleFileNode(1, FileNodeKind.Namespace, namespaceFiles.Key, files);
        foreach (var file in files)
        {
            yield return new VisibleFileNode(2, FileNodeKind.File, Path.GetFileName(file.Path), [file]);
        }
    }
}
