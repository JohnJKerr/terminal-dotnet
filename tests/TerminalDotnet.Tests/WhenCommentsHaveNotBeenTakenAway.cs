using TerminalDotnet.Comments;
using Xunit;

namespace TerminalDotnet.Tests.Comments;

public sealed class WhenCommentsHaveNotBeenTakenAway
{
    [Fact]
    public void It_has_nothing_outstanding_before_anything_is_commented()
    {
        // Arrange
        var session = new CommentSession();

        // Act
        var state = session.State;

        // Assert
        Assert.False(state.Unsaved);
    }

    [Fact]
    public async Task It_has_the_note_outstanding_once_one_is_written()
    {
        // Arrange
        var session = new CommentSession();

        // Act
        await session.DispatchAsync(
            new CommentCommand.Add("/repo/src/Order.cs", "src/Order.cs", "needs a guard"));

        // Assert
        Assert.True(session.State.Unsaved);
    }

    [Fact]
    public async Task It_has_nothing_outstanding_once_the_notes_are_copied()
    {
        // Arrange
        var session = await SessionWithANote(new TakingClipboard());

        // Act
        await session.DispatchAsync(new CommentCommand.CopyAll());

        // Assert
        Assert.False(session.State.Unsaved);
    }

    [Fact]
    public async Task It_keeps_the_note_outstanding_when_the_clipboard_refuses_it()
    {
        // Arrange
        var session = await SessionWithANote(new RefusingClipboard());

        // Act
        await session.DispatchAsync(new CommentCommand.CopyAll());

        // Assert
        Assert.True(session.State.Unsaved);
    }

    [Fact]
    public async Task It_has_nothing_outstanding_once_the_notes_are_saved()
    {
        // Arrange
        var session = await SessionWithANote(store: new TakingStore());

        // Act
        await session.DispatchAsync(new CommentCommand.SaveAll("comments.md"));

        // Assert
        Assert.False(session.State.Unsaved);
    }

    [Fact]
    public async Task It_puts_the_notes_back_outstanding_once_one_is_rewritten()
    {
        // Arrange
        var session = await SessionWithANote(new TakingClipboard());
        await session.DispatchAsync(new CommentCommand.CopyAll());

        // Act
        await session.DispatchAsync(new CommentCommand.RewriteSelected("needs two guards"));

        // Assert
        Assert.True(session.State.Unsaved);
    }

    [Fact]
    public async Task It_has_nothing_outstanding_once_every_note_is_cleared()
    {
        // Arrange
        var session = await SessionWithANote(new TakingClipboard());

        // Act
        await session.DispatchAsync(new CommentCommand.ClearAll());

        // Assert
        Assert.False(session.State.Unsaved);
    }

    [Fact]
    public async Task It_leaves_the_notes_alone_when_the_panel_is_only_searched()
    {
        // Arrange
        var session = await SessionWithANote(new TakingClipboard());
        await session.DispatchAsync(new CommentCommand.CopyAll());

        // Act
        await session.DispatchAsync(new CommentCommand.Search("order"));

        // Assert
        Assert.False(session.State.Unsaved);
    }

    private static async Task<CommentSession> SessionWithANote(
        ICommentClipboard? clipboard = null,
        ICommentStore? store = null)
    {
        var session = new CommentSession(clipboard, store);
        await session.DispatchAsync(
            new CommentCommand.Add("/repo/src/Order.cs", "src/Order.cs", "needs a guard"));
        return session;
    }

    private sealed class TakingClipboard : ICommentClipboard
    {
        public Task<bool> TryCopyAsync(
            string text,
            CancellationToken cancellationToken = default) => Task.FromResult(true);
    }

    private sealed class RefusingClipboard : ICommentClipboard
    {
        public Task<bool> TryCopyAsync(
            string text,
            CancellationToken cancellationToken = default) => Task.FromResult(false);
    }

    private sealed class TakingStore : ICommentStore
    {
        public Task<bool> ExistsAsync(
            string path,
            CancellationToken cancellationToken = default) => Task.FromResult(false);

        public Task<bool> TryWriteAsync(
            string path,
            string text,
            CancellationToken cancellationToken = default) => Task.FromResult(true);
    }
}
