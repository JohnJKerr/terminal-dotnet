namespace TerminalDotnet.Comments;

/// <summary>
/// The comments as one piece of text, headed by the file each is against, so
/// they can be pasted into a review or kept as a file.
/// </summary>
public static class CommentReport
{
    public static string From(IReadOnlyList<FileComment> comments) =>
        string.Join('\n', comments.Select(SectionFor));

    private static string SectionFor(FileComment comment) =>
        $"## {comment.DisplayPath}\n\n{comment.Text}\n";
}
