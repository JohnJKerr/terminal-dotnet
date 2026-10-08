using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Mouse;

/// <summary>What a drag has selected is marked over the preview, a stretch
/// to each line on show, placed by the columns it is drawn in.</summary>
public sealed class WhenMarkingSelectedPreviewText
{
    private static readonly IReadOnlyList<string> Lines =
    [
        "public sealed class Cart",
        "{",
        "    private int total;",
        "}"
    ];

    [Fact]
    public void It_marks_the_cells_the_drag_covers()
    {
        // Arrange
        var selection = new PreviewSelection(new PreviewCell(0, 7), new PreviewCell(0, 12));

        // Act
        var marked = selection.MarkedIn(Lines, top: 0, height: 4);

        // Assert
        Assert.Equal([new SelectedStretch(0, 7, "sealed")], marked);
    }

    [Fact]
    public void It_marks_a_stretch_on_each_line_of_a_longer_drag()
    {
        // Arrange
        var selection = new PreviewSelection(new PreviewCell(0, 14), new PreviewCell(2, 10));

        // Act
        var marked = selection.MarkedIn(Lines, top: 0, height: 4);

        // Assert
        Assert.Equal(
            [new SelectedStretch(0, 14, "class Cart"), new SelectedStretch(1, 0, "{"), new SelectedStretch(2, 0, "    private")],
            marked);
    }

    [Fact]
    public void It_marks_only_the_lines_on_show()
    {
        // Arrange
        var selection = new PreviewSelection(new PreviewCell(0, 14), new PreviewCell(2, 10));

        // Act
        var marked = selection.MarkedIn(Lines, top: 1, height: 1);

        // Assert
        Assert.Equal([new SelectedStretch(1, 0, "{")], marked);
    }

    [Fact]
    public void It_marks_a_tab_as_wide_as_it_is_drawn()
    {
        // Arrange
        var selection = new PreviewSelection(new PreviewCell(0, 2), new PreviewCell(0, 6), TabStop: 4);

        // Act
        var marked = selection.MarkedIn(["\treturn total;"], top: 0, height: 1);

        // Assert
        Assert.Equal([new SelectedStretch(0, 0, "    ret")], marked);
    }

    [Fact]
    public void It_marks_nothing_on_a_line_with_no_text()
    {
        // Arrange
        var selection = new PreviewSelection(new PreviewCell(0, 0), new PreviewCell(2, 0));

        // Act
        var marked = selection.MarkedIn(["a", "", "b"], top: 1, height: 1);

        // Assert
        Assert.Empty(marked);
    }

    [Fact]
    public void A_click_without_a_drag_marks_nothing()
    {
        // Arrange
        var selection = new PreviewSelection(new PreviewCell(0, 7), new PreviewCell(0, 7));

        // Act
        var marked = selection.MarkedIn(Lines, top: 0, height: 4);

        // Assert
        Assert.Empty(marked);
    }
}
