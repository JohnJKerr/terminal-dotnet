using TerminalDotnet.Filters;
using TerminalDotnet.Search;

namespace TerminalDotnet.Files;

public sealed class FileExplorerSession(
    IFileExplorerBackend backend,
    FileGrouping grouping = FileGrouping.Project)
{
    private IReadOnlyList<FileTreeNode> tree = [];
    private IReadOnlyList<FileEntry> discoveredFiles = [];
    private readonly HashSet<string> collapsedNodes = [];

    public FileExplorerState State { get; private set; } = new([]) { Loading = true };

    public async Task LoadAsync(string target, CancellationToken cancellationToken = default)
    {
        try
        {
            State = await DiscoveredStateAsync(target, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            State = new FileExplorerState([]);
        }
    }

    /// <summary>A reload can arrive while the reader is part-way through the
    /// tree, so what they searched for, filtered to and were standing on is
    /// carried across rather than dropped back to the top of an unfiltered list.
    /// </summary>
    private async Task<FileExplorerState> DiscoveredStateAsync(
        string target,
        CancellationToken cancellationToken)
    {
        var standingOn = SelectedKey();
        var files = Snapshot.Of(await backend.DiscoverAsync(target, cancellationToken));
        discoveredFiles = files;
        tree = TreeOf(FilesMatching(State.SearchQuery, State.ActiveFilter));
        var nodes = VisibleNodes();

        return State with
        {
            VisibleNodes = nodes,
            SelectedIndex = RowFor(standingOn, nodes.Count),
            Changes = SummaryFrom(files),
            Loading = false
        };
    }

    private string? SelectedKey() =>
        VisibleKeys() is { } keys && State.SelectedIndex < keys.Count
            ? keys[State.SelectedIndex]
            : null;

    /// <summary>The row the reader was on, wherever it has moved to. A row that
    /// the edit took away leaves them where they were standing instead.
    /// </summary>
    private int RowFor(string? key, int rowCount)
    {
        var moved = key is null ? -1 : VisibleKeys().IndexOf(key);
        return moved >= 0 ? moved : Math.Clamp(State.SelectedIndex, 0, Math.Max(0, rowCount - 1));
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
            case FileExplorerCommand.ToggleExpanded:
                ToggleSelectedExpansion();
                return;
            case FileExplorerCommand.ToggleAllExpanded:
                ToggleWholeTreeExpansion();
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
        tree = TreeOf(FilesMatching(query, filter));
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

        Collapse(VisibleKeys()[State.SelectedIndex]);
        State = State with { VisibleNodes = VisibleNodes() };
    }

    private void ToggleWholeTreeExpansion()
    {
        var groupKeys = GroupKeys();
        var collapseAll = groupKeys.Any(key => !collapsedNodes.Contains(key));
        collapsedNodes.Clear();
        if (collapseAll)
        {
            collapsedNodes.UnionWith(groupKeys);
        }

        var nodes = VisibleNodes();
        State = State with
        {
            VisibleNodes = nodes,
            SelectedIndex = Math.Min(State.SelectedIndex, Math.Max(0, nodes.Count - 1))
        };
    }

    private IReadOnlyList<string> GroupKeys() => tree
        .Where(node => node.Node.Kind != FileNodeKind.File)
        .Select(node => node.Key)
        .ToArray();

    private void Collapse(string key)
    {
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
                FileExplorerCommand.SelectIndex jump => Math.Clamp(jump.Index, 0, lastIndex),
                FileExplorerCommand.MoveUp => Math.Max(0, State.SelectedIndex - 1),
                FileExplorerCommand.MoveDown => Math.Min(lastIndex, State.SelectedIndex + 1),
                _ => State.SelectedIndex
            }
        };
    }

    private IReadOnlyList<VisibleFileNode> VisibleNodes() => Snapshot.Of(
        Unfolded().Select(node => node.Node with { IsExpanded = IsExpanded(node.Key) }));

    private List<string> VisibleKeys() =>
        [.. Unfolded().Select(node => node.Key)];

    private IReadOnlyList<FileTreeNode> Unfolded()
    {
        var visible = new List<FileTreeNode>();
        int? hiddenBelowDepth = null;
        foreach (var node in tree)
        {
            if (hiddenBelowDepth is not null && node.Node.Depth > hiddenBelowDepth)
            {
                continue;
            }

            hiddenBelowDepth = IsExpanded(node.Key) ? null : node.Node.Depth;
            visible.Add(node);
        }

        return visible;
    }

    private bool IsExpanded(string key) => !collapsedNodes.Contains(key);

    private IReadOnlyList<FileEntry> FilesMatching(string query, ExplorerFilter? filter) =>
        Snapshot.Of(discoveredFiles
            .Where(file => SearchMatch.Matches(file.Path, query))
            .Where(file => PassesFilter(file, filter)));

    private static bool PassesFilter(FileEntry file, ExplorerFilter? filter) =>
        filter != ExplorerFilter.Updated || file.GitStatus != FileGitStatus.Unchanged;

    private static FileChangeSummary SummaryFrom(IReadOnlyList<FileEntry> files) => new(
        files.Count(file => file.GitStatus != FileGitStatus.Deleted),
        files.Count(file => file.GitStatus == FileGitStatus.New),
        files.Count(file => file.GitStatus == FileGitStatus.Modified),
        files.Count(file => file.GitStatus == FileGitStatus.Deleted));

    private IReadOnlyList<FileTreeNode> TreeOf(IReadOnlyList<FileEntry> files) =>
        FileTree.Of(files, grouping);
}
