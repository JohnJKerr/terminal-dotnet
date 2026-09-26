using TerminalDotnet.Comments;

namespace TerminalDotnet.Terminal;

public sealed record CommentPanelSnapshot(
    IReadOnlyList<FileComment> Comments,
    int SelectedIndex,
    string SearchQuery,
    int SearchHitCount,
    IReadOnlyList<StatusSegment> StatusSegments,
    string EmptyMessage)
{
    public IReadOnlyList<PanelRow> Rows => Snapshot.Of(Comments.Select(RowFrom));

    public static CommentPanelSnapshot From(CommentsState state) => new(
        state.Comments,
        state.SelectedIndex,
        state.SearchQuery,
        state.Comments.Count,
        StatusSegmentsFrom(state),
        PanelEmptyState.For("comments", state.Comments.Count, state.SearchQuery));

    private static IReadOnlyList<StatusSegment> StatusSegmentsFrom(CommentsState state) =>
    [
        new($"{state.Comments.Count} Commented", RowTone.Neutral),
        .. state.Notice.Length > 0
            ? new StatusSegment[] { new(state.Notice, RowTone.Neutral) }
            : []
    ];

    /// <summary>A row reads as the file and the note's opening line, so the
    /// listing says what was said without unfolding the whole comment.</summary>
    private static PanelRow RowFrom(FileComment comment) => new(
        $"{comment.DisplayPath} — {OpeningLineOf(comment.Text)}",
        RowTone.Neutral);

    private static string OpeningLineOf(string text) =>
        text.Split('\n', 2)[0].TrimEnd();
}
