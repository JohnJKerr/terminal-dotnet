using TerminalDotnet.Comments;
using TerminalDotnet.Tests.Fakes;

namespace TerminalDotnet.Tests.Builders;

/// <summary>Arranges a comment session holding the notes a test needs. Each
/// note is left against a file under /repo, named by its path from there.
/// </summary>
internal sealed class CommentSessionBuilder
{
    private readonly List<CommentCommand.Add> notes = [];
    private ICommentClipboard clipboard = new RecordingClipboard();
    private ICommentStore store = new RecordingCommentStore();

    public CommentSessionBuilder WithClipboard(ICommentClipboard used)
    {
        clipboard = used;
        return this;
    }

    public CommentSessionBuilder WithStore(ICommentStore used)
    {
        store = used;
        return this;
    }

    public CommentSessionBuilder WithNote(string displayPath, string text)
    {
        notes.Add(new CommentCommand.Add($"/repo/{displayPath}", displayPath, text));
        return this;
    }

    /// <summary>The order note first, so the customer note sorts ahead of it.
    /// </summary>
    public CommentSessionBuilder WithTwoNotes() => this
        .WithNote("src/Order.cs", "needs a guard")
        .WithNote("src/Customer.cs", "rename this");

    public CommentSession Build() => new(clipboard, store);

    public async Task<CommentSession> BuildAsync()
    {
        var session = Build();
        foreach (var note in notes)
        {
            await session.DispatchAsync(note);
        }

        return session;
    }
}
