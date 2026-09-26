using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Presentation;

public sealed class WhenHighlightingAPreviewLine
{
    [Fact]
    public void It_places_the_selected_source_row_in_the_viewport()
    {
        // Arrange
        const string source = "first\nsecond\nthird";

        // Act
        var highlight = PreviewSourceHighlight.From(source, 3, 1, 2, enabled: true);

        // Assert
        Assert.Equal(new PreviewSourceHighlight("third", 1, true), highlight);
    }

    [Fact]
    public void It_hides_the_selected_row_when_scrolled_out_of_view()
    {
        // Arrange
        const string source = "first\nsecond\nthird";

        // Act
        var highlight = PreviewSourceHighlight.From(source, 1, 1, 2, enabled: true);

        // Assert
        Assert.False(highlight.Visible);
    }
}
