using TerminalDotnet.Changes;

namespace TerminalDotnet.Terminal;

public sealed record ChangesetPanelSnapshot(
    IReadOnlyList<ChangedFile> Files,
    int SelectedIndex,
    string SearchQuery,
    int SearchHitCount,
    IReadOnlyList<StatusSegment> StatusSegments,
    DiffContext? Diff,
    string EmptyMessage)
{
    public IReadOnlyList<PanelRow> Rows => Snapshot.Of(Files.Select(RowFrom));

    public string DiffTitle => Diff?.DisplayPath ?? "";

    public IReadOnlyList<DiffLine> DiffLines => DiffAppearance.LinesFrom(Diff?.Diff ?? "");

    public static ChangesetPanelSnapshot From(ChangesetState state) => new(
        state.Files,
        state.SelectedIndex,
        state.SearchQuery,
        state.Files.Count,
        StatusSegmentsFrom(state.Summary, state.Notice),
        state.Diff,
        EmptyMessageFrom(state));

    private static string EmptyMessageFrom(ChangesetState state) => state.Loading
        ? ""
        : PanelEmptyState.For("changes", state.Files.Count, state.SearchQuery);

    private static IReadOnlyList<StatusSegment> StatusSegmentsFrom(
        ChangesetSummary summary,
        string notice) =>
    [
        new($"{summary.Changed} Changed", RowTone.Modified),
        new($"{summary.Added} Added", RowTone.New),
        new($"{summary.Deleted} Deleted", RowTone.Deleted),
        .. notice.Length > 0 ? new StatusSegment[] { new(notice, RowTone.Deleted) } : []
    ];

    private static PanelRow RowFrom(ChangedFile file) => new(
        $"{MarkerFor(file.Kind)} {file.DisplayPath}",
        ToneFor(file.Kind));

    private static string MarkerFor(ChangeKind kind) => kind switch
    {
        ChangeKind.Added => "+",
        ChangeKind.Deleted => "-",
        _ => "~"
    };

    private static RowTone ToneFor(ChangeKind kind) => kind switch
    {
        ChangeKind.Added => RowTone.New,
        ChangeKind.Deleted => RowTone.Deleted,
        _ => RowTone.Modified
    };
}
