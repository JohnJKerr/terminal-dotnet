using TerminalDotnet.Comments;
using Xunit;

namespace TerminalDotnet.Tests.Comments;

public sealed class WhenClearingEveryComment
{
    [Fact]
    public async Task It_leaves_no_notes_behind()
    {
        // Arrange
        var session = await SessionWithTwoComments();

        // Act
        await session.DispatchAsync(new CommentCommand.ClearAll());

        // Assert
        Assert.Empty(session.State.Comments);
    }

    [Fact]
    public async Task It_forgets_the_note_a_file_was_carrying()
    {
        // Arrange
        var session = await SessionWithTwoComments();

        // Act
        await session.DispatchAsync(new CommentCommand.ClearAll());

        // Assert
        Assert.Equal("", session.Against("/repo/src/Order.cs"));
    }

    [Fact]
    public async Task It_says_how_many_notes_were_cleared()
    {
        // Arrange
        var session = await SessionWithTwoComments();

        // Act
        await session.DispatchAsync(new CommentCommand.ClearAll());

        // Assert
        Assert.Equal("Cleared 2 comments", session.State.Notice);
    }

    [Fact]
    public async Task It_clears_the_notes_a_search_has_hidden_as_well()
    {
        // Arrange
        var session = await SessionWithTwoComments();
        await session.DispatchAsync(new CommentCommand.Search("order"));

        // Act
        await session.DispatchAsync(new CommentCommand.ClearAll());

        // Assert
        Assert.Equal("", session.Against("/repo/src/Customer.cs"));
    }

    [Fact]
    public async Task It_says_nothing_when_there_was_nothing_to_clear()
    {
        // Arrange
        var session = new CommentSession();

        // Act
        await session.DispatchAsync(new CommentCommand.ClearAll());

        // Assert
        Assert.Equal("", session.State.Notice);
    }

    private static async Task<CommentSession> SessionWithTwoComments()
    {
        var session = new CommentSession();
        await session.DispatchAsync(
            new CommentCommand.Add("/repo/src/Order.cs", "src/Order.cs", "needs a guard"));
        await session.DispatchAsync(
            new CommentCommand.Add("/repo/src/Customer.cs", "src/Customer.cs", "rename this"));
        return session;
    }
}
