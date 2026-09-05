using TerminalDotnet.Filters;
using TerminalDotnet.Search;

namespace TerminalDotnet.Files;

public sealed class FileExplorerSession(IFileExplorerBackend backend)
{
    private IReadOnlyList<VisibleFileNode> allNodes = [];
    private IReadOnlyList<FileEntry> discoveredFiles = [];
    private readonly HashSet<string> collapsedNodes = [];

    public FileExplorerState State { get; private set; } = new([]);

    public async Task LoadAsync(string target, CancellationToken cancellationToken = default)
    {
        var files = await backend.DiscoverAsync(target, cancellationToken);
        discoveredFiles = files;
        allNodes = NodesFrom(files);
        State = new FileExplorerState(allNodes) { Changes = SummaryFrom(files) };
    }

    public Task DispatchAsync(FileExplorerCommand command)
    {
        Apply(command);
        return Task.CompletedTask;
    }

    private void Apply(FileExplorerCommand command)
    {
        switch (command)
        {
            case FileExplorerCommand.Search search:
                ApplySearch(search.Query);
                return;
            case FileExplorerCommand.ClearSearch:
                ApplySearch("");
                return;
            case FileExplorerCommand.ToggleFilter filter:
                ApplyFilter(filter.Filter);
                return;
            case FileExplorerCommand.NextSearchMatch:
                Select(SelectionRing.Next(FileIndices(), State.SelectedIndex));
                return;
            case FileExplorerCommand.PreviousSearchMatch:
                Select(SelectionRing.Previous(FileIndices(), State.SelectedIndex));
                return;
            case FileExplorerCommand.ToggleExpanded:
                ToggleSelectedExpansion();
                return;
            default:
                MoveSelection(command);
                return;
        }
    }

    private void ApplySearch(string query) => Show(query, State.ActiveFilter);

    private void ApplyFilter(ExplorerFilter filter) =>
        Show(State.SearchQuery, State.ActiveFilter == filter ? null : filter);

    private void Show(string query, ExplorerFilter? filter)
    {
        allNodes = NodesFrom(FilesMatching(query, filter));
        State = State with
        {
            VisibleNodes = VisibleNodes(),
            SelectedIndex = 0,
            SearchQuery = query,
            ActiveFilter = filter
        };
    }

    private void ToggleSelectedExpansion()
    {
        if (State.VisibleNodes.Count == 0)
        {
            return;
        }

        Collapse(State.VisibleNodes[State.SelectedIndex]);
        State = State with { VisibleNodes = VisibleNodes() };
    }

    private void Collapse(VisibleFileNode node)
    {
        var key = NodeKey(node);
        if (!collapsedNodes.Add(key))
        {
            collapsedNodes.Remove(key);
        }
    }

    private void MoveSelection(FileExplorerCommand command)
    {
        var lastIndex = Math.Max(0, State.VisibleNodes.Count - 1);
        State = State with
        {
            SelectedIndex = command switch
            {
                FileExplorerCommand.MoveUp => Math.Max(0, State.SelectedIndex - 1),
                FileExplorerCommand.MoveDown => Math.Min(lastIndex, State.SelectedIndex + 1),
                _ => State.SelectedIndex
            }
        };
    }

    private void Select(int index)
    {
        if (index == SelectionRing.None)
        {
            return;
        }

        State = State with { SelectedIndex = index };
    }

    private IReadOnlyList<VisibleFileNode> VisibleNodes()
    {
        var visible = new List<VisibleFileNode>();
        int? hiddenBelowDepth = null;
        foreach (var node in allNodes)
        {
            if (hiddenBelowDepth is not null && node.Depth > hiddenBelowDepth)
            {
                continue;
            }

            hiddenBelowDepth = null;
            var isExpanded = !collapsedNodes.Contains(NodeKey(node));
            visible.Add(node with { IsExpanded = isExpanded });
            if (!isExpanded)
            {
                hiddenBelowDepth = node.Depth;
            }
        }

        return visible;
    }

    private static string NodeKey(VisibleFileNode node) =>
        $"{node.Kind}:{node.Files[0].ProjectPath}:{node.Name}";

    private IReadOnlyList<FileEntry> FilesMatching(string query, ExplorerFilter? filter) =>
        discoveredFiles
            .Where(file => SearchMatch.Matches(file.Path, query))
            .Where(file => PassesFilter(file, filter))
            .ToArray();

    private static bool PassesFilter(FileEntry file, ExplorerFilter? filter) =>
        filter != ExplorerFilter.Updated || file.GitStatus != FileGitStatus.Unchanged;

    private IReadOnlyList<int> FileIndices() => State.VisibleNodes
        .Select((node, index) => (node, index))
        .Where(item => item.node.Kind == FileNodeKind.File)
        .Select(item => item.index)
        .ToArray();

    private static FileChangeSummary SummaryFrom(IReadOnlyList<FileEntry> files) => new(
        files.Count(file => file.GitStatus != FileGitStatus.Deleted),
        files.Count(file => file.GitStatus == FileGitStatus.New),
        files.Count(file => file.GitStatus == FileGitStatus.Modified),
        files.Count(file => file.GitStatus == FileGitStatus.Deleted));

    private static IReadOnlyList<VisibleFileNode> NodesFrom(IReadOnlyList<FileEntry> files) => files
        .Where(file => file.GitStatus != FileGitStatus.Deleted)
        .GroupBy(file => file.ProjectPath)
        .OrderBy(group => group.Key, StringComparer.Ordinal)
        .SelectMany(ProjectNodes)
        .ToArray();

    private static IEnumerable<VisibleFileNode> ProjectNodes(IGrouping<string, FileEntry> project)
    {
        var projectFiles = project.ToArray();
        var projectNode = new VisibleFileNode(
            0,
            FileNodeKind.Project,
            Path.GetFileNameWithoutExtension(project.Key),
            projectFiles);
        var namespaceNodes = project
            .GroupBy(file => file.Namespace)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .SelectMany(NamespaceNodes);

        return [projectNode, .. namespaceNodes];
    }

    private static IEnumerable<VisibleFileNode> NamespaceNodes(IGrouping<string, FileEntry> namespaceFiles)
    {
        var files = namespaceFiles.OrderBy(file => file.Path, StringComparer.Ordinal).ToArray();
        var namespaceNode = new VisibleFileNode(1, FileNodeKind.Namespace, namespaceFiles.Key, files);
        var fileNodes = files
            .Select(file => new VisibleFileNode(2, FileNodeKind.File, Path.GetFileName(file.Path), [file]));

        return [namespaceNode, .. fileNodes];
    }
}
