using TerminalDotnet.Comments;
using TerminalDotnet.Tests.Builders;
using Xunit;

namespace TerminalDotnet.Tests.Comments;

public sealed class WhenSearchingTheComments
{
    [Fact]
    public async Task It_shows_only_the_files_whose_path_matches()
    {
        // Arrange
        var session = await GivenA.CommentSession().WithTwoNotes().BuildAsync();

        // Act
        await session.DispatchAsync(new CommentCommand.Search("order"));

        // Assert
        Assert.Equal(
            ["src/Order.cs"],
            session.State.Comments.Select(comment => comment.DisplayPath));
    }

    [Fact]
    public async Task It_shows_the_files_whose_note_matches()
    {
        // Arrange
        var session = await GivenA.CommentSession().WithTwoNotes().BuildAsync();

        // Act
        await session.DispatchAsync(new CommentCommand.Search("rename"));

        // Assert
        Assert.Equal(
            ["src/Customer.cs"],
            session.State.Comments.Select(comment => comment.DisplayPath));
    }

    [Fact]
    public async Task It_remembers_what_was_searched_for()
    {
        // Arrange
        var session = await GivenA.CommentSession().WithTwoNotes().BuildAsync();

        // Act
        await session.DispatchAsync(new CommentCommand.Search("rename"));

        // Assert
        Assert.Equal("rename", session.State.SearchQuery);
    }

    [Fact]
    public async Task It_returns_to_the_first_row_when_the_search_changes()
    {
        // Arrange
        var session = await GivenA.CommentSession().WithTwoNotes().BuildAsync();
        await session.DispatchAsync(new CommentCommand.MoveDown());

        // Act
        await session.DispatchAsync(new CommentCommand.Search(""));

        // Assert
        Assert.Equal(0, session.State.SelectedIndex);
    }

    [Fact]
    public async Task It_brings_every_note_back_when_the_search_is_cleared()
    {
        // Arrange
        var session = await GivenA.CommentSession().WithTwoNotes().BuildAsync();
        await session.DispatchAsync(new CommentCommand.Search("order"));

        // Act
        await session.DispatchAsync(new CommentCommand.ClearSearch());

        // Assert
        Assert.Equal(2, session.State.Comments.Count);
    }

    [Fact]
    public async Task It_keeps_a_new_note_out_of_a_search_it_does_not_match()
    {
        // Arrange
        var session = await GivenA.CommentSession().WithTwoNotes().BuildAsync();
        await session.DispatchAsync(new CommentCommand.Search("order"));

        // Act
        await session.DispatchAsync(
            new CommentCommand.Add("/repo/src/Basket.cs", "src/Basket.cs", "split this up"));

        // Assert
        Assert.Equal(
            ["src/Order.cs"],
            session.State.Comments.Select(comment => comment.DisplayPath));
    }
}
