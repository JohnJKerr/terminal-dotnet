namespace TerminalDotnet.Comments;

/// <summary>A note the reader left against one file while looking at it.</summary>
public sealed record FileComment(string Path, string DisplayPath, string Text);

public sealed record CommentsState(IReadOnlyList<FileComment> Comments, int SelectedIndex = 0)
{
    /// <summary>The note a file already carries, so commenting on it again
    /// starts from what is there rather than from a blank page.</summary>
    public string Against(string path) =>
        Comments.FirstOrDefault(comment => comment.Path == path)?.Text ?? "";
}

public abstract record CommentCommand
{
    public sealed record Add(string Path, string DisplayPath, string Text) : CommentCommand;
    public sealed record RewriteSelected(string Text) : CommentCommand;
    public sealed record DeleteSelected : CommentCommand;
    public sealed record MoveUp : CommentCommand;
    public sealed record MoveDown : CommentCommand;
}
