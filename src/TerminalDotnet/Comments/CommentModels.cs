namespace TerminalDotnet.Comments;

/// <summary>A note the reader left against one file while looking at it.</summary>
public sealed record FileComment(string Path, string DisplayPath, string Text);

public sealed record CommentsState(
    IReadOnlyList<FileComment> Comments,
    int SelectedIndex = 0,
    string SearchQuery = "")
{
    /// <summary>What became of the last thing the reader asked for, shown
    /// beside the count until the next command.</summary>
    public string Notice { get; init; } = "";
}

/// <summary>Where a comment goes when it is taken out of the app.</summary>
public interface ICommentClipboard
{
    bool TryCopy(string text);
}

public abstract record CommentCommand
{
    public sealed record Add(string Path, string DisplayPath, string Text) : CommentCommand;
    public sealed record Search(string Query) : CommentCommand;
    public sealed record ClearSearch : CommentCommand;
    public sealed record RewriteSelected(string Text) : CommentCommand;
    public sealed record DeleteSelected : CommentCommand;
    public sealed record CopyAll : CommentCommand;
    public sealed record MoveUp : CommentCommand;
    public sealed record MoveDown : CommentCommand;
}
