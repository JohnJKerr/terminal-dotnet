using TerminalDotnet.Changes;

namespace TerminalDotnet.Terminal;

public sealed record ChangesetPanelRow(string Text, FileRowTone Tone);

public sealed record ChangesetPanelSnapshot(
    IReadOnlyList<ChangesetPanelRow> Rows,
    int SelectedIndex,
    string SearchQuery,
    int SearchHitCount,
    IReadOnlyList<FileStatusSegment> StatusSegments,
    string DiffTitle,
    IReadOnlyList<DiffLine> DiffLines,
    string EmptyMessage)
{
    public static ChangesetPanelSnapshot From(ChangesetState state) => new(
        state.Files.Select(RowFrom).ToArray(),
        state.SelectedIndex,
        state.SearchQuery,
        state.Files.Count,
        StatusSegmentsFrom(state.Summary),
        state.Diff?.DisplayPath ?? "",
        DiffAppearance.LinesFrom(state.Diff?.Diff ?? ""),
        PanelEmptyState.For("changes", state.Files.Count, state.SearchQuery));

    private static IReadOnlyList<FileStatusSegment> StatusSegmentsFrom(ChangesetSummary summary) =>
    [
        new($"{summary.Changed} Changed", FileRowTone.Modified),
        new($"{summary.Added} Added", FileRowTone.New),
        new($"{summary.Deleted} Deleted", FileRowTone.Deleted)
    ];

    private static ChangesetPanelRow RowFrom(ChangedFile file) => new(
        $"{MarkerFor(file.Kind)} {file.DisplayPath}",
        ToneFor(file.Kind));

    private static string MarkerFor(ChangeKind kind) => kind switch
    {
        ChangeKind.Added => "+",
        ChangeKind.Deleted => "-",
        _ => "~"
    };

    private static FileRowTone ToneFor(ChangeKind kind) => kind switch
    {
        ChangeKind.Added => FileRowTone.New,
        ChangeKind.Deleted => FileRowTone.Deleted,
        _ => FileRowTone.Modified
    };
}
