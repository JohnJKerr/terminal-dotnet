using TerminalDotnet.Comments;
using Xunit;

namespace TerminalDotnet.Tests.Comments;

public sealed class WhenTakingTheCommentsAway
{
    [Fact]
    public async Task It_copies_every_note_to_the_clipboard()
    {
        // Arrange
        var clipboard = new InMemoryClipboard();
        var session = await SessionWithTwoComments(clipboard);

        // Act
        await session.DispatchAsync(new CommentCommand.CopyAll());

        // Assert
        Assert.Equal(
            "## src/Customer.cs\n\nrename this\n\n## src/Order.cs\n\nneeds a guard\n",
            clipboard.Copied);
    }

    [Fact]
    public async Task It_copies_the_notes_a_search_has_hidden_as_well()
    {
        // Arrange
        var clipboard = new InMemoryClipboard();
        var session = await SessionWithTwoComments(clipboard);
        await session.DispatchAsync(new CommentCommand.Search("order"));

        // Act
        await session.DispatchAsync(new CommentCommand.CopyAll());

        // Assert
        Assert.Contains("src/Customer.cs", clipboard.Copied);
    }

    [Fact]
    public async Task It_says_how_many_notes_were_copied()
    {
        // Arrange
        var session = await SessionWithTwoComments(new InMemoryClipboard());

        // Act
        await session.DispatchAsync(new CommentCommand.CopyAll());

        // Assert
        Assert.Equal("Copied 2 comments", session.State.Notice);
    }

    [Fact]
    public async Task It_says_so_when_the_clipboard_would_not_take_them()
    {
        // Arrange
        var session = await SessionWithTwoComments(new InMemoryClipboard { CopySucceeds = false });

        // Act
        await session.DispatchAsync(new CommentCommand.CopyAll());

        // Assert
        Assert.Equal("Could not copy the comments", session.State.Notice);
    }

    [Fact]
    public async Task It_leaves_the_clipboard_alone_when_nothing_is_commented()
    {
        // Arrange
        var clipboard = new InMemoryClipboard();
        var session = new CommentSession(clipboard);

        // Act
        await session.DispatchAsync(new CommentCommand.CopyAll());

        // Assert
        Assert.Null(clipboard.Copied);
    }

    private static async Task<CommentSession> SessionWithTwoComments(ICommentClipboard clipboard)
    {
        var session = new CommentSession(clipboard);
        await session.DispatchAsync(
            new CommentCommand.Add("/repo/src/Order.cs", "src/Order.cs", "needs a guard"));
        await session.DispatchAsync(
            new CommentCommand.Add("/repo/src/Customer.cs", "src/Customer.cs", "rename this"));
        return session;
    }

    private sealed class InMemoryClipboard : ICommentClipboard
    {
        public string? Copied { get; private set; }

        public bool CopySucceeds { get; init; } = true;

        public bool TryCopy(string text)
        {
            if (!CopySucceeds)
            {
                return false;
            }

            Copied = text;
            return true;
        }
    }
}
