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

    [Fact]
    public async Task It_writes_every_note_to_the_file()
    {
        // Arrange
        var store = new InMemoryCommentStore();
        var session = await SessionWithTwoComments(store);

        // Act
        await session.DispatchAsync(new CommentCommand.SaveAll("comments.md"));

        // Assert
        Assert.Equal(
            "## src/Customer.cs\n\nrename this\n\n## src/Order.cs\n\nneeds a guard\n",
            store.Written);
    }

    [Fact]
    public async Task It_writes_the_notes_a_search_has_hidden_as_well()
    {
        // Arrange
        var store = new InMemoryCommentStore();
        var session = await SessionWithTwoComments(store);
        await session.DispatchAsync(new CommentCommand.Search("order"));

        // Act
        await session.DispatchAsync(new CommentCommand.SaveAll("comments.md"));

        // Assert
        Assert.Contains("src/Customer.cs", store.Written);
    }

    [Fact]
    public async Task It_says_where_the_notes_were_saved()
    {
        // Arrange
        var session = await SessionWithTwoComments(new InMemoryCommentStore());

        // Act
        await session.DispatchAsync(new CommentCommand.SaveAll("comments.md"));

        // Assert
        Assert.Equal("Saved 2 comments to comments.md", session.State.Notice);
    }

    [Fact]
    public async Task It_says_so_when_the_notes_could_not_be_saved()
    {
        // Arrange
        var session = await SessionWithTwoComments(
            new InMemoryCommentStore { WriteSucceeds = false });

        // Act
        await session.DispatchAsync(new CommentCommand.SaveAll("comments.md"));

        // Assert
        Assert.Equal("Could not save the comments to comments.md", session.State.Notice);
    }

    [Fact]
    public async Task It_leaves_the_file_alone_when_nothing_is_commented()
    {
        // Arrange
        var store = new InMemoryCommentStore();
        var session = new CommentSession(store: store);

        // Act
        await session.DispatchAsync(new CommentCommand.SaveAll("comments.md"));

        // Assert
        Assert.Null(store.Written);
    }

    private static async Task<CommentSession> SessionWithTwoComments(ICommentStore store)
    {
        var session = new CommentSession(store: store);
        await session.DispatchAsync(
            new CommentCommand.Add("/repo/src/Order.cs", "src/Order.cs", "needs a guard"));
        await session.DispatchAsync(
            new CommentCommand.Add("/repo/src/Customer.cs", "src/Customer.cs", "rename this"));
        return session;
    }

    private sealed class InMemoryCommentStore : ICommentStore
    {
        public string? Written { get; private set; }

        public bool WriteSucceeds { get; init; } = true;

        public Task<bool> TryWriteAsync(
            string path,
            string text,
            CancellationToken cancellationToken = default)
        {
            if (!WriteSucceeds)
            {
                return Task.FromResult(false);
            }

            Written = text;
            return Task.FromResult(true);
        }
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

        public Task<bool> TryCopyAsync(string text, CancellationToken cancellationToken = default)
        {
            if (!CopySucceeds)
            {
                return Task.FromResult(false);
            }

            Copied = text;
            return Task.FromResult(true);
        }
    }
}
