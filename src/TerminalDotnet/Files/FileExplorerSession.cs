using TerminalDotnet.Filters;
using TerminalDotnet.Search;

namespace TerminalDotnet.Files;

public sealed class FileExplorerSession(
    IFileExplorerBackend backend,
    FileGrouping grouping = FileGrouping.Project)
{
    private IReadOnlyList<FileTreeNode> tree = [];
    private IReadOnlyList<FileEntry> discoveredFiles = [];
    private readonly FoldedGroups folded = new();

    public FileExplorerState State { get; private set; } = new([]) { Loading = true };

    public async Task LoadAsync(string target, CancellationToken cancellationToken = default)
    {
        try
        {
            State = await DiscoveredStateAsync(target, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            State = new FileExplorerState([]) { Notice = $"Could not read the files: {exception.Message}" };
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
        var unfolded = Unfolded();

        return State with
        {
            VisibleNodes = VisibleNodesOf(unfolded),
            SelectedIndex = RowSelection.FoundAgain(unfolded, node => node.Key == standingOn, State.SelectedIndex),
            Changes = SummaryFrom(files),
            Loading = false,
            Notice = ""
        };
    }

    private string? SelectedKey() => SelectedTreeNode()?.Key;

    private FileTreeNode? SelectedTreeNode() =>
        Unfolded() is { } unfolded && State.SelectedIndex < unfolded.Count
            ? unfolded[State.SelectedIndex]
            : null;

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
        if (SelectedTreeNode() is not { } selected)
        {
            return;
        }

        folded.Toggle(selected.Key);
        State = State with { VisibleNodes = VisibleNodes() };
    }

    private void ToggleWholeTreeExpansion()
    {
        folded.ToggleAll(GroupKeys());
        var nodes = VisibleNodes();
        State = State with
        {
            VisibleNodes = nodes,
            SelectedIndex = RowSelection.Kept(State.SelectedIndex, nodes.Count)
        };
    }

    private IReadOnlyList<string> GroupKeys() => tree
        .Where(node => node.Node.Kind != FileNodeKind.File)
        .Select(node => node.Key)
        .ToArray();

    private void MoveSelection(FileExplorerCommand command)
    {
        var rowCount = State.VisibleNodes.Count;
        State = State with
        {
            SelectedIndex = command switch
            {
                FileExplorerCommand.SelectIndex jump => RowSelection.At(jump.Index, rowCount),
                FileExplorerCommand.MoveUp => RowSelection.Up(State.SelectedIndex),
                FileExplorerCommand.MoveDown => RowSelection.Down(State.SelectedIndex, rowCount),
                _ => State.SelectedIndex
            }
        };
    }

    private IReadOnlyList<VisibleFileNode> VisibleNodes() => VisibleNodesOf(Unfolded());

    private IReadOnlyList<VisibleFileNode> VisibleNodesOf(IReadOnlyList<FileTreeNode> unfolded) => Snapshot.Of(
        unfolded.Select(node => node.Node with { IsExpanded = IsExpanded(node.Key) }));

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

    private bool IsExpanded(string key) => folded.IsExpanded(key);

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
