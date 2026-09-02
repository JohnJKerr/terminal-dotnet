using Terminal.Gui.Drawing;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Explorer;

public sealed class WhenStylingSourcePreview
{
    [Fact]
    public void It_gives_keywords_and_types_distinct_colours()
    {
        // Act
        var colours = new[]
        {
            PreviewCodeAppearance.ForegroundFor(VisualRole.CodeKeyword),
            PreviewCodeAppearance.ForegroundFor(VisualRole.CodeType)
        };

        // Assert
        Assert.Equal(2, colours.Distinct().Count());
    }
}
