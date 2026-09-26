using Terminal.Gui.Drawing;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Presentation;

public sealed class WhenStylingExplorerFiles
{
    [Fact]
    public void A_deleted_count_is_red()
    {
        // Act
        var foreground = RowAppearance.ForegroundFor(RowTone.Deleted, Color.White);

        // Assert
        Assert.Equal(Color.BrightRed, foreground);
    }

    [Fact]
    public void A_total_count_keeps_the_unchanged_colour()
    {
        // Act
        var foreground = RowAppearance.ForegroundFor(RowTone.Neutral, Color.White);

        // Assert
        Assert.Equal(Color.White, foreground);
    }
}
