using TerminalDotnet.Files;

namespace TerminalDotnet.Terminal;

public sealed record FilePanelSnapshot(
    IReadOnlyList<VisibleFileNode> Nodes,
    int SelectedIndex,
    string SearchQuery,
    int SearchHitCount,
    IReadOnlyList<StatusSegment> StatusSegments,
    IReadOnlyList<FilterChip> Filters,
    string EmptyMessage)
{
    public IReadOnlyList<PanelRow> Rows => Snapshot.Of(Nodes.Select(RowFrom));

    public static FilePanelSnapshot From(FileExplorerState state, bool showsAllFiles = false) => new(
        state.VisibleNodes,
        state.SelectedIndex,
        state.SearchQuery,
        state.VisibleFileCount,
        StatusSegmentsFrom(state.Changes, state.Notice),
        [new FilterChip("A All files", showsAllFiles), .. PanelFilters.Chips(state.ActiveFilter)],
        EmptyMessageFrom(state));

    private static string EmptyMessageFrom(FileExplorerState state) => state.Loading
        ? ""
        : PanelEmptyState.For(
            "files",
            state.VisibleNodes.Count,
            state.SearchQuery,
            state.ActiveFilter);

    private static IReadOnlyList<StatusSegment> StatusSegmentsFrom(FileChangeSummary changes, string notice) =>
    [
        new(CountedNoun.Of(changes.Total, "File"), RowTone.Neutral),
        new($"{changes.Added} Added", RowTone.New),
        new($"{changes.Edited} Edited", RowTone.Modified),
        new($"{changes.Deleted} Deleted", RowTone.Deleted),
        .. notice.Length > 0 ? new StatusSegment[] { new(notice, RowTone.Deleted) } : []
    ];

    private static PanelRow RowFrom(VisibleFileNode node) => new(
        $"{new string(' ', node.Depth * 2)}{MarkerFor(node)} {node.Name}",
        ToneFor(node));

    private static string MarkerFor(VisibleFileNode node)
    {
        if (node.Kind == FileNodeKind.File)
        {
            return "•";
        }

        return node.IsExpanded ? "▼" : "▶";
    }

    private static RowTone ToneFor(VisibleFileNode node)
    {
        if (node.Kind != FileNodeKind.File)
        {
            return RowTone.Neutral;
        }

        return node.Files[0].GitStatus switch
        {
            FileGitStatus.Modified => RowTone.Modified,
            FileGitStatus.New => RowTone.New,
            FileGitStatus.Deleted => RowTone.Deleted,
            _ => RowTone.Neutral
        };
    }
}
