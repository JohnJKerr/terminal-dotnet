using TerminalDotnet.Search;

namespace TerminalDotnet.Comments;

/// <summary>
/// The comments left in this sitting. They live for as long as the app does,
/// so the reader can gather notes across the panels and take them away in one
/// go rather than writing them down somewhere else.
/// </summary>
public sealed class CommentSession(ICommentClipboard? clipboard = null, ICommentStore? store = null)
{
    private readonly ICommentClipboard clipboard = clipboard ?? new UnreachableClipboard();
    private readonly ICommentStore store = store ?? new UnreachableStore();
    private readonly List<FileComment> comments = [];

    /// <summary>Set once the notes have been copied or saved, and cleared the
    /// moment one of them changes again.</summary>
    private bool takenAway;

    public CommentsState State { get; private set; } = new([]);

    /// <summary>The note a file already carries, whatever the panel is showing,
    /// so commenting on it again starts from what is there rather than from a
    /// blank page.</summary>
    public string Against(string path) =>
        comments.FirstOrDefault(comment => comment.Path == path)?.Text ?? "";

    /// <summary>Whether saving to this path would write over something. The
    /// write itself replaces what is there, so the asking is done first.
    /// </summary>
    public Task<bool> HoldsSomethingAtAsync(
        string path,
        CancellationToken cancellationToken = default) => store.ExistsAsync(path, cancellationToken);

    public async Task DispatchAsync(
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

        var notice = command is CommentCommand.ClearAll
            ? Cleared()
            : await NoticeForAsync(command, cancellationToken);
        var query = QueryAfter(command);
        var listed = Matching(query);
        State = new CommentsState(listed, SelectionAfter(command, listed.Count), query)
        {
            Notice = notice,
            Unsaved = comments.Count > 0 && !takenAway
        };
    }

    /// <summary>Clearing takes the whole book, not the page the panel is
    /// showing, so a search cannot leave notes behind that nobody asked to
    /// keep.</summary>
    private string Cleared()
    {
        var cleared = comments.Count;
        comments.Clear();
        return cleared == 0 ? "" : $"Cleared {CountedNoun.Of(cleared, "comment")}";
    }

    /// <summary>Taking the comments away takes all of them, not only the ones
    /// the panel is showing, because a search narrows the reading rather than
    /// the record.</summary>
    private async Task<string> NoticeForAsync(
        CommentCommand command,
        CancellationToken cancellationToken)
    {
        if (comments.Count == 0)
        {
            return "";
        }

        if (command is CommentCommand.CopyAll)
        {
            var copied = await clipboard.TryCopyAsync(
                CommentReport.From(InPathOrder()),
                cancellationToken);
            takenAway |= copied;
            return copied ? $"Copied {Counted()}" : "Could not copy the comments";
        }

        if (command is not CommentCommand.SaveAll save)
        {
            return "";
        }

        var saved = await store.TryWriteAsync(
            save.Path,
            CommentReport.From(InPathOrder()),
            cancellationToken);
        takenAway |= saved;
        return saved
            ? $"Saved {Counted()} to {save.Path}"
            : $"Could not save the comments to {save.Path}";
    }

    private string Counted() => CountedNoun.Of(comments.Count, "comment");

    private IReadOnlyList<FileComment> InPathOrder() => Matching("");

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
        takenAway = false;
    }

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
        takenAway = false;
    }

    private string QueryAfter(CommentCommand command) => command switch
    {
        CommentCommand.Search search => search.Query,
        CommentCommand.ClearSearch => "",
        _ => State.SearchQuery
    };

    /// <summary>A note is found by the file it is against or by what it says,
    /// because the reader remembers one or the other.</summary>
    private IReadOnlyList<FileComment> Matching(string query) => Snapshot.Of(comments
        .Where(comment => query.Length == 0 || Matches(comment, query))
        .OrderBy(comment => comment.DisplayPath, StringComparer.Ordinal));

    private static bool Matches(FileComment comment, string query) =>
        SearchMatch.Matches(comment.DisplayPath, query) ||
        SearchMatch.Matches(comment.Text, query);

    private int SelectionAfter(CommentCommand command, int count)
    {
        var lastIndex = Math.Max(0, count - 1);
        return command switch
        {
            CommentCommand.Search or CommentCommand.ClearSearch => 0,
            CommentCommand.SelectIndex jump => Math.Clamp(jump.Index, 0, lastIndex),
            CommentCommand.MoveUp => Math.Max(0, State.SelectedIndex - 1),
            CommentCommand.MoveDown => Math.Min(lastIndex, State.SelectedIndex + 1),
            _ => Math.Min(State.SelectedIndex, lastIndex)
        };
    }

    private sealed class UnreachableClipboard : ICommentClipboard
    {
        public Task<bool> TryCopyAsync(
            string text,
            CancellationToken cancellationToken = default) => Task.FromResult(false);
    }

    private sealed class UnreachableStore : ICommentStore
    {
        public Task<bool> ExistsAsync(
            string path,
            CancellationToken cancellationToken = default) => Task.FromResult(false);

        public Task<bool> TryWriteAsync(
            string path,
            string text,
            CancellationToken cancellationToken = default) => Task.FromResult(false);
    }
}
