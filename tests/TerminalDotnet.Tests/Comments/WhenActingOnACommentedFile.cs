using TerminalDotnet.Comments;
using TerminalDotnet.Tests.Builders;
using Xunit;

namespace TerminalDotnet.Tests.Comments;

public sealed class WhenActingOnACommentedFile
{
    [Fact]
    public async Task It_rewrites_the_note_against_the_selected_file()
    {
        // Arrange
        var session = await GivenA.CommentSession().WithTwoNotes().BuildAsync();
        await session.DispatchAsync(new CommentCommand.MoveDown());

        // Act
        await session.DispatchAsync(new CommentCommand.RewriteSelected("needs two guards"));

        // Assert
        Assert.Equal("needs two guards", session.Against("/repo/src/Order.cs"));
    }

    [Fact]
    public async Task It_leaves_the_other_notes_alone_when_one_is_rewritten()
    {
        // Arrange
        var session = await GivenA.CommentSession().WithTwoNotes().BuildAsync();
        await session.DispatchAsync(new CommentCommand.MoveDown());

        // Act
        await session.DispatchAsync(new CommentCommand.RewriteSelected("needs two guards"));

        // Assert
        Assert.Equal("rename this", session.Against("/repo/src/Customer.cs"));
    }

    [Fact]
    public async Task It_drops_the_note_against_the_selected_file_when_it_is_deleted()
    {
        // Arrange
        var session = await GivenA.CommentSession().WithTwoNotes().BuildAsync();

        // Act
        await session.DispatchAsync(new CommentCommand.DeleteSelected());

        // Assert
        Assert.Equal(
            ["src/Order.cs"],
            session.State.Comments.Select(comment => comment.DisplayPath));
    }

    [Fact]
    public async Task It_steps_back_onto_the_last_note_when_the_bottom_one_is_deleted()
    {
        // Arrange
        var session = await GivenA.CommentSession().WithTwoNotes().BuildAsync();
        await session.DispatchAsync(new CommentCommand.MoveDown());

        // Act
        await session.DispatchAsync(new CommentCommand.DeleteSelected());

        // Assert
        Assert.Equal(0, session.State.SelectedIndex);
    }

    [Fact]
    public async Task It_drops_the_note_altogether_when_it_is_rewritten_to_nothing()
    {
        // Arrange
        var session = await GivenA.CommentSession().WithTwoNotes().BuildAsync();

        // Act
        await session.DispatchAsync(new CommentCommand.RewriteSelected("   "));

        // Assert
        Assert.Equal(
            ["src/Order.cs"],
            session.State.Comments.Select(comment => comment.DisplayPath));
    }
}
