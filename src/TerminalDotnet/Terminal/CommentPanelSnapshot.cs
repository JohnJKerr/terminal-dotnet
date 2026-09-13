using TerminalDotnet.Comments;

namespace TerminalDotnet.Terminal;

public sealed record CommentPanelRow(string Text, FileRowTone Tone);

public sealed record CommentPanelSnapshot(
    IReadOnlyList<FileComment> Comments,
    int SelectedIndex,
    IReadOnlyList<FileStatusSegment> StatusSegments,
    string EmptyMessage)
{
    public IReadOnlyList<CommentPanelRow> Rows => Snapshot.Of(Comments.Select(RowFrom));

    public static CommentPanelSnapshot From(CommentsState state) => new(
        state.Comments,
        state.SelectedIndex,
        [new FileStatusSegment($"{state.Comments.Count} Commented", FileRowTone.Neutral)],
        PanelEmptyState.For("comments", state.Comments.Count, ""));

    /// <summary>A row reads as the file and the note's opening line, so the
    /// listing says what was said without unfolding the whole comment.</summary>
    private static CommentPanelRow RowFrom(FileComment comment) => new(
        $"{comment.DisplayPath} — {OpeningLineOf(comment.Text)}",
        FileRowTone.Neutral);

    private static string OpeningLineOf(string text) =>
        text.Split('\n', 2)[0].TrimEnd();
}
