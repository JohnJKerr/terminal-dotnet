using TerminalDotnet.Comments;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Presentation;

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

    [Fact]
    public void It_says_nothing_matches_a_search_that_found_no_notes()
    {
        // Arrange
        var state = new CommentsState([], SearchQuery: "basket");

        // Act
        var snapshot = CommentPanelSnapshot.From(state);

        // Assert
        Assert.Equal("No comments match 'basket'", snapshot.EmptyMessage);
    }

    [Fact]
    public void It_keeps_the_search_the_panel_is_showing()
    {
        // Arrange
        var state = new CommentsState(Comments(), SearchQuery: "src");

        // Act
        var snapshot = CommentPanelSnapshot.From(state);

        // Assert
        Assert.Equal("src", snapshot.SearchQuery);
    }

    [Fact]
    public void It_counts_the_notes_a_search_found()
    {
        // Arrange
        var state = new CommentsState(Comments(), SearchQuery: "src");

        // Act
        var snapshot = CommentPanelSnapshot.From(state);

        // Assert
        Assert.Equal(2, snapshot.SearchHitCount);
    }

    [Fact]
    public void It_shows_a_notice_on_the_status_line()
    {
        // Arrange
        var state = new CommentsState(Comments()) { Notice = "Copied 2 comments" };

        // Act
        var snapshot = CommentPanelSnapshot.From(state);

        // Assert
        Assert.Equal(
            ["2 Commented", "Copied 2 comments"],
            snapshot.StatusSegments.Select(segment => segment.Text));
    }

    private static IReadOnlyList<FileComment> Comments() =>
    [
        new("/repo/src/Customer.cs", "src/Customer.cs", "rename this"),
        new("/repo/src/Order.cs", "src/Order.cs", "needs a guard")
    ];
}
