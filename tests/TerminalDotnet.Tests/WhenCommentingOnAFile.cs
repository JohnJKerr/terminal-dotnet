using TerminalDotnet.Comments;
using Xunit;

namespace TerminalDotnet.Tests.Comments;

public sealed class WhenCommentingOnAFile
{
    [Fact]
    public async Task It_keeps_the_comment_against_the_file()
    {
        // Arrange
        var session = new CommentSession();

        // Act
        await session.DispatchAsync(
            new CommentCommand.Add("/repo/src/Order.cs", "src/Order.cs", "needs a guard"));

        // Assert
        Assert.Equal(
            [new FileComment("/repo/src/Order.cs", "src/Order.cs", "needs a guard")],
            session.State.Comments);
    }

    [Fact]
    public async Task It_keeps_one_comment_against_a_file_that_is_commented_twice()
    {
        // Arrange
        var session = new CommentSession();
        await session.DispatchAsync(
            new CommentCommand.Add("/repo/src/Order.cs", "src/Order.cs", "needs a guard"));

        // Act
        await session.DispatchAsync(
            new CommentCommand.Add("/repo/src/Order.cs", "src/Order.cs", "needs two guards"));

        // Assert
        Assert.Equal(
            ["needs two guards"],
            session.State.Comments.Select(comment => comment.Text));
    }

    [Fact]
    public async Task It_leaves_no_comment_when_nothing_was_written()
    {
        // Arrange
        var session = new CommentSession();

        // Act
        await session.DispatchAsync(
            new CommentCommand.Add("/repo/src/Order.cs", "src/Order.cs", "   "));

        // Assert
        Assert.Empty(session.State.Comments);
    }

    [Fact]
    public async Task It_lists_the_commented_files_in_path_order()
    {
        // Arrange
        var session = new CommentSession();
        await session.DispatchAsync(
            new CommentCommand.Add("/repo/src/Order.cs", "src/Order.cs", "needs a guard"));

        // Act
        await session.DispatchAsync(
            new CommentCommand.Add("/repo/src/Customer.cs", "src/Customer.cs", "rename this"));

        // Assert
        Assert.Equal(
            ["src/Customer.cs", "src/Order.cs"],
            session.State.Comments.Select(comment => comment.DisplayPath));
    }

    [Fact]
    public async Task It_finds_the_note_already_left_against_a_file()
    {
        // Arrange
        var session = new CommentSession();
        await session.DispatchAsync(
            new CommentCommand.Add("/repo/src/Order.cs", "src/Order.cs", "needs a guard"));

        // Act
        var existing = session.State.Against("/repo/src/Order.cs");

        // Assert
        Assert.Equal("needs a guard", existing);
    }

    [Fact]
    public async Task It_finds_nothing_against_a_file_nobody_has_commented_on()
    {
        // Arrange
        var session = new CommentSession();
        await session.DispatchAsync(
            new CommentCommand.Add("/repo/src/Order.cs", "src/Order.cs", "needs a guard"));

        // Act
        var existing = session.State.Against("/repo/src/Customer.cs");

        // Assert
        Assert.Equal("", existing);
    }
}
