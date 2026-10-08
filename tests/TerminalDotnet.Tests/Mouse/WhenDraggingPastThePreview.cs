using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Mouse;

/// <summary>A selection can run on past what the preview has on show: a
/// drag held beyond its top or bottom scrolls the text under it, the faster
/// the further out it is held.</summary>
public sealed class WhenDraggingPastThePreview
{
    [Fact]
    public void A_drag_held_within_the_text_leaves_it_where_it_is()
    {
        // Act
        var rows = PreviewDragScroll.RowsFor(row: 9, height: 10);

        // Assert
        Assert.Equal(0, rows);
    }

    [Fact]
    public void A_drag_held_just_below_the_text_scrolls_one_row_forward()
    {
        // Act
        var rows = PreviewDragScroll.RowsFor(row: 10, height: 10);

        // Assert
        Assert.Equal(1, rows);
    }

    [Fact]
    public void A_drag_held_just_above_the_text_scrolls_one_row_back()
    {
        // Act
        var rows = PreviewDragScroll.RowsFor(row: -1, height: 10);

        // Assert
        Assert.Equal(-1, rows);
    }

    [Fact]
    public void A_drag_held_further_out_scrolls_faster()
    {
        // Act
        var rows = PreviewDragScroll.RowsFor(row: 12, height: 10);

        // Assert
        Assert.Equal(3, rows);
    }

    [Fact]
    public void A_drag_held_far_below_scrolls_no_faster_than_the_reader_can_follow()
    {
        // Act
        var rows = PreviewDragScroll.RowsFor(row: 40, height: 10);

        // Assert
        Assert.Equal(PreviewDragScroll.MostRows, rows);
    }

    [Fact]
    public void A_drag_held_far_above_scrolls_back_no_faster()
    {
        // Act
        var rows = PreviewDragScroll.RowsFor(row: -40, height: 10);

        // Assert
        Assert.Equal(-PreviewDragScroll.MostRows, rows);
    }
}
