using TerminalDotnet.Comments;
using Xunit;

namespace TerminalDotnet.Tests.Comments;

public sealed class WhenBrowsingTheComments
{
    [Fact]
    public async Task It_starts_on_the_first_commented_file()
    {
        // Arrange
        var session = await SessionWithTwoComments();

        // Act
        var state = session.State;

        // Assert
        Assert.Equal(0, state.SelectedIndex);
    }

    [Fact]
    public async Task It_selects_the_next_file_when_moving_down()
    {
        // Arrange
        var session = await SessionWithTwoComments();

        // Act
        await session.DispatchAsync(new CommentCommand.MoveDown());

        // Assert
        Assert.Equal("src/Order.cs", session.State.Comments[session.State.SelectedIndex].DisplayPath);
    }

    [Fact]
    public async Task It_selects_the_previous_file_when_moving_up()
    {
        // Arrange
        var session = await SessionWithTwoComments();
        await session.DispatchAsync(new CommentCommand.MoveDown());

        // Act
        await session.DispatchAsync(new CommentCommand.MoveUp());

        // Assert
        Assert.Equal(
            "src/Customer.cs",
            session.State.Comments[session.State.SelectedIndex].DisplayPath);
    }

    [Fact]
    public async Task It_stays_on_the_last_file_at_the_bottom_of_the_list()
    {
        // Arrange
        var session = await SessionWithTwoComments();
        await session.DispatchAsync(new CommentCommand.MoveDown());

        // Act
        await session.DispatchAsync(new CommentCommand.MoveDown());

        // Assert
        Assert.Equal(1, session.State.SelectedIndex);
    }

    [Fact]
    public async Task It_stays_on_the_first_file_at_the_top_of_the_list()
    {
        // Arrange
        var session = await SessionWithTwoComments();

        // Act
        await session.DispatchAsync(new CommentCommand.MoveUp());

        // Assert
        Assert.Equal(0, session.State.SelectedIndex);
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
