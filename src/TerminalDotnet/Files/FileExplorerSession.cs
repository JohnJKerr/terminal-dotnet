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
        if (command is FileExplorerCommand.Search search)
        {
            allNodes = NodesFrom(discoveredFiles.Where(file => IsOrderedMatch(file.Path, search.Query)).ToArray());
            State = State with
            {
                VisibleNodes = VisibleNodes(),
                SelectedIndex = 0,
                SearchQuery = search.Query
            };
            return Task.CompletedTask;
        }

        if (command is FileExplorerCommand.ClearSearch)
        {
            allNodes = NodesFrom(discoveredFiles);
            State = State with
            {
                VisibleNodes = VisibleNodes(),
                SelectedIndex = 0,
                SearchQuery = ""
            };
            return Task.CompletedTask;
        }

        if (command is FileExplorerCommand.NextSearchMatch)
        {
            var matchIndices = FileNodeIndices();
            var nextMatch = matchIndices.FirstOrDefault(
                index => index > State.SelectedIndex,
                matchIndices.FirstOrDefault(-1));
            if (nextMatch >= 0)
            {
                State = State with { SelectedIndex = nextMatch };
            }

            return Task.CompletedTask;
        }

        if (command is FileExplorerCommand.PreviousSearchMatch)
        {
            var matchIndices = FileNodeIndices();
            var previousMatch = matchIndices.LastOrDefault(
                index => index < State.SelectedIndex,
                matchIndices.LastOrDefault(-1));
            if (previousMatch >= 0)
            {
                State = State with { SelectedIndex = previousMatch };
            }

            return Task.CompletedTask;
        }

        if (command is FileExplorerCommand.ToggleExpanded && State.VisibleNodes.Count > 0)
        {
            var selected = State.VisibleNodes[State.SelectedIndex];
            var key = NodeKey(selected);
            if (!collapsedNodes.Add(key))
            {
                collapsedNodes.Remove(key);
            }

            State = State with { VisibleNodes = VisibleNodes(), SelectedIndex = 0 };
        }

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

        return Task.CompletedTask;
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

    private static bool IsOrderedMatch(string candidate, string query)
    {
        var queryIndex = 0;
        foreach (var character in candidate)
        {
            if (queryIndex < query.Length &&
                char.ToUpperInvariant(character) == char.ToUpperInvariant(query[queryIndex]))
            {
                queryIndex++;
            }
        }

        return queryIndex == query.Length;
    }

    private int[] FileNodeIndices() => State.VisibleNodes
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
