using TerminalDotnet.Comments;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Comments;

public sealed class WhenCreatingACommentPanelSnapshot
{
    [Fact]
    public void It_lists_the_file_every_comment_was_left_against()
    {
        // Arrange
        var state = new CommentsState(Comments());

        // Act
        var snapshot = CommentPanelSnapshot.From(state);

        // Assert
        Assert.Equal(
            ["src/Customer.cs — rename this", "src/Order.cs — needs a guard"],
            snapshot.Rows.Select(row => row.Text));
    }

    [Fact]
    public void It_shows_only_the_opening_line_of_a_comment_that_runs_on()
    {
        // Arrange
        var state = new CommentsState(
            [new FileComment("/repo/src/Order.cs", "src/Order.cs", "needs a guard\nand a test")]);

        // Act
        var snapshot = CommentPanelSnapshot.From(state);

        // Assert
        Assert.Equal(["src/Order.cs — needs a guard"], snapshot.Rows.Select(row => row.Text));
    }

    [Fact]
    public void It_counts_the_commented_files_on_the_status_line()
    {
        // Arrange
        var state = new CommentsState(Comments());

        // Act
        var snapshot = CommentPanelSnapshot.From(state);

        // Assert
        Assert.Equal(["2 Commented"], snapshot.StatusSegments.Select(segment => segment.Text));
    }

    [Fact]
    public void It_says_there_is_nothing_to_show_before_anything_is_commented()
    {
        // Arrange
        var state = new CommentsState([]);

        // Act
        var snapshot = CommentPanelSnapshot.From(state);

        // Assert
        Assert.Equal("No comments to show", snapshot.EmptyMessage);
    }

    [Fact]
    public void It_keeps_the_row_the_panel_is_on()
    {
        // Arrange
        var state = new CommentsState(Comments(), SelectedIndex: 1);

        // Act
        var snapshot = CommentPanelSnapshot.From(state);

        // Assert
        Assert.Equal(1, snapshot.SelectedIndex);
    }

    private static IReadOnlyList<FileComment> Comments() =>
    [
        new("/repo/src/Customer.cs", "src/Customer.cs", "rename this"),
        new("/repo/src/Order.cs", "src/Order.cs", "needs a guard")
    ];
}
