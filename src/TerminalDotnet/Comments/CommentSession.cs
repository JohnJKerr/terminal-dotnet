namespace TerminalDotnet.Comments;

/// <summary>
/// The comments left in this sitting. They live for as long as the app does,
/// so the reader can gather notes across the panels and take them away in one
/// go rather than writing them down somewhere else.
/// </summary>
public sealed class CommentSession
{
    private readonly List<FileComment> comments = [];

    public CommentsState State { get; private set; } = new([]);

    public Task DispatchAsync(
        CommentCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is CommentCommand.Add add)
        {
            Write(add);
        }

        if (Selected() is { } selected)
        {
            Revise(command, selected);
        }

        var listed = InPathOrder();
        State = new CommentsState(listed, SelectionAfter(command, listed.Count));
        return Task.CompletedTask;
    }

    private int SelectionAfter(CommentCommand command, int count)
    {
        var lastIndex = Math.Max(0, count - 1);
        return command switch
        {
            CommentCommand.MoveUp => Math.Max(0, State.SelectedIndex - 1),
            CommentCommand.MoveDown => Math.Min(lastIndex, State.SelectedIndex + 1),
            _ => Math.Min(State.SelectedIndex, lastIndex)
        };
    }

    private FileComment? Selected() => State.SelectedIndex < State.Comments.Count
        ? State.Comments[State.SelectedIndex]
        : null;

    private void Revise(CommentCommand command, FileComment selected)
    {
        if (command is CommentCommand.RewriteSelected rewrite)
        {
            Rewrite(selected, rewrite.Text);
            return;
        }

        if (command is CommentCommand.DeleteSelected)
        {
            Erase(selected);
        }
    }

    /// <summary>Rubbing a note out is how it is taken back, so a comment
    /// rewritten to nothing leaves its file uncommented.</summary>
    private void Rewrite(FileComment selected, string text)
    {
        if (text.Trim().Length == 0)
        {
            Erase(selected);
            return;
        }

        Write(new CommentCommand.Add(selected.Path, selected.DisplayPath, text));
    }

    private void Erase(FileComment selected)
    {
        comments.RemoveAll(comment => comment.Path == selected.Path);
    }

    /// <summary>The commented files read as a listing rather than as a history,
    /// so a note that is rewritten keeps the place its file has.</summary>
    private IReadOnlyList<FileComment> InPathOrder() => Snapshot.Of(
        comments.OrderBy(comment => comment.DisplayPath, StringComparer.Ordinal));

    /// <summary>A file carries one comment, so commenting on it again rewrites
    /// the note that is already there rather than leaving two behind.</summary>
    private void Write(CommentCommand.Add add)
    {
        var text = add.Text.Trim();
        if (text.Length == 0)
        {
            return;
        }

        comments.RemoveAll(comment => comment.Path == add.Path);
        comments.Add(new FileComment(add.Path, add.DisplayPath, text));
    }
}
