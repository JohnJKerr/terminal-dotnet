using TerminalDotnet.Comments;
using TerminalDotnet.Tests.Builders;
using TerminalDotnet.Tests.Fakes;
using Xunit;

namespace TerminalDotnet.Tests.Comments;

public sealed class WhenCommentsHaveNotBeenTakenAway
{
    [Fact]
    public void It_has_nothing_outstanding_before_anything_is_commented()
    {
        // Arrange
        var session = GivenA.CommentSession().Build();

        // Act
        var state = session.State;

        // Assert
        Assert.False(state.Unsaved);
    }

    [Fact]
    public async Task It_has_the_note_outstanding_once_one_is_written()
    {
        // Arrange
        var session = GivenA.CommentSession().Build();

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
        var session = await GivenA.CommentSession()
            .WithNote("src/Order.cs", "needs a guard")
            .WithClipboard(new RecordingClipboard())
            .BuildAsync();

        // Act
        await session.DispatchAsync(new CommentCommand.CopyAll());

        // Assert
        Assert.False(session.State.Unsaved);
    }

    [Fact]
    public async Task It_keeps_the_note_outstanding_when_the_clipboard_refuses_it()
    {
        // Arrange
        var session = await GivenA.CommentSession()
            .WithNote("src/Order.cs", "needs a guard")
            .WithClipboard(new RecordingClipboard { CopySucceeds = false })
            .BuildAsync();

        // Act
        await session.DispatchAsync(new CommentCommand.CopyAll());

        // Assert
        Assert.True(session.State.Unsaved);
    }

    [Fact]
    public async Task It_has_nothing_outstanding_once_the_notes_are_saved()
    {
        // Arrange
        var session = await GivenA.CommentSession().WithNote("src/Order.cs", "needs a guard").BuildAsync();

        // Act
        await session.DispatchAsync(new CommentCommand.SaveAll("comments.md"));

        // Assert
        Assert.False(session.State.Unsaved);
    }

    [Fact]
    public async Task It_puts_the_notes_back_outstanding_once_one_is_rewritten()
    {
        // Arrange
        var session = await GivenA.CommentSession()
            .WithNote("src/Order.cs", "needs a guard")
            .WithClipboard(new RecordingClipboard())
            .BuildAsync();
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
        var session = await GivenA.CommentSession()
            .WithNote("src/Order.cs", "needs a guard")
            .WithClipboard(new RecordingClipboard())
            .BuildAsync();

        // Act
        await session.DispatchAsync(new CommentCommand.ClearAll());

        // Assert
        Assert.False(session.State.Unsaved);
    }

    [Fact]
    public async Task It_leaves_the_notes_alone_when_the_panel_is_only_searched()
    {
        // Arrange
        var session = await GivenA.CommentSession()
            .WithNote("src/Order.cs", "needs a guard")
            .WithClipboard(new RecordingClipboard())
            .BuildAsync();
        await session.DispatchAsync(new CommentCommand.CopyAll());

        // Act
        await session.DispatchAsync(new CommentCommand.Search("order"));

        // Assert
        Assert.False(session.State.Unsaved);
    }
}
