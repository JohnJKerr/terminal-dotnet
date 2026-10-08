using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Mouse;

/// <summary>A drag across the preview selects the text between the cell the
/// button went down on and the cell it was let go on, both included.</summary>
public sealed class WhenSelectingPreviewText
{
    private static readonly IReadOnlyList<string> Lines =
    [
        "public sealed class Cart",
        "{",
        "    private int total;",
        "}"
    ];

    [Fact]
    public void Dragging_along_a_line_takes_the_text_between_the_two_cells()
    {
        // Arrange
        var selection = new PreviewSelection(new PreviewCell(0, 7), new PreviewCell(0, 12));

        // Act
        var text = selection.TextIn(Lines);

        // Assert
        Assert.Equal("sealed", text);
    }

    [Fact]
    public void Dragging_backwards_takes_the_same_text()
    {
        // Arrange
        var selection = new PreviewSelection(new PreviewCell(0, 12), new PreviewCell(0, 7));

        // Act
        var text = selection.TextIn(Lines);

        // Assert
        Assert.Equal("sealed", text);
    }

    [Fact]
    public void Dragging_down_takes_the_lines_in_between_whole()
    {
        // Arrange
        var selection = new PreviewSelection(new PreviewCell(0, 14), new PreviewCell(2, 10));

        // Act
        var text = selection.TextIn(Lines);

        // Assert
        Assert.Equal("class Cart\n{\n    private", text);
    }

    [Fact]
    public void Dragging_up_takes_the_same_lines()
    {
        // Arrange
        var selection = new PreviewSelection(new PreviewCell(2, 10), new PreviewCell(0, 14));

        // Act
        var text = selection.TextIn(Lines);

        // Assert
        Assert.Equal("class Cart\n{\n    private", text);
    }

    [Fact]
    public void Dragging_past_the_end_of_a_line_stops_at_its_end()
    {
        // Arrange
        var selection = new PreviewSelection(new PreviewCell(0, 14), new PreviewCell(0, 80));

        // Act
        var text = selection.TextIn(Lines);

        // Assert
        Assert.Equal("class Cart", text);
    }

    [Fact]
    public void Dragging_below_the_last_line_stops_at_the_end_of_the_text()
    {
        // Arrange
        var selection = new PreviewSelection(new PreviewCell(2, 16), new PreviewCell(9, 3));

        // Act
        var text = selection.TextIn(Lines);

        // Assert
        Assert.Equal("total;\n}", text);
    }

    [Fact]
    public void A_click_without_a_drag_selects_nothing()
    {
        // Arrange
        var selection = new PreviewSelection(new PreviewCell(0, 7), new PreviewCell(0, 7));

        // Act
        var text = selection.TextIn(Lines);

        // Assert
        Assert.Equal("", text);
    }

    [Fact]
    public void A_drag_that_ends_on_half_of_a_wide_character_takes_all_of_it()
    {
        // Arrange
        var selection = new PreviewSelection(new PreviewCell(0, 3), new PreviewCell(0, 5));

        // Act
        var text = selection.TextIn(["// 合計 total"]);

        // Assert
        Assert.Equal("合計", text);
    }

    [Fact]
    public void A_tab_is_taken_as_the_tab_it_is_however_wide_it_is_drawn()
    {
        // Arrange
        var selection = new PreviewSelection(new PreviewCell(0, 2), new PreviewCell(0, 6), TabStop: 4);

        // Act
        var text = selection.TextIn(["\treturn total;"]);

        // Assert
        Assert.Equal("\tret", text);
    }
}
