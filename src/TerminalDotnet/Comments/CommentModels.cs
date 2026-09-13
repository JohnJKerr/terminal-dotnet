namespace TerminalDotnet.Comments;

/// <summary>A note the reader left against one file while looking at it.</summary>
public sealed record FileComment(string Path, string DisplayPath, string Text);

public sealed record CommentsState(IReadOnlyList<FileComment> Comments);

public abstract record CommentCommand
{
    public sealed record Add(string Path, string DisplayPath, string Text) : CommentCommand;
}
