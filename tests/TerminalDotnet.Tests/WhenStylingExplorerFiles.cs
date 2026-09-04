using Terminal.Gui.Drawing;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Explorer;

public sealed class WhenStylingExplorerFiles
{
    [Fact]
    public void A_selected_modified_file_keeps_its_blue_status_and_selection_background()
    {
        // Arrange
        var normal = new global::Terminal.Gui.Drawing.Attribute(Color.White, Color.Black);
        var selected = new global::Terminal.Gui.Drawing.Attribute(Color.Black, Color.BrightYellow);

        // Act
        var appearance = FileRowAppearance.For(FileRowTone.Modified, isSelected: true, normal, selected);

        // Assert
        Assert.Equal(
            (Color.BrightBlue, Color.BrightYellow),
            (appearance.Foreground, appearance.Background));
    }

    [Fact]
    public void A_deleted_count_is_red()
    {
        // Act
        var foreground = FileRowAppearance.ForegroundFor(FileRowTone.Deleted, Color.White);

        // Assert
        Assert.Equal(Color.BrightRed, foreground);
    }

    [Fact]
    public void A_total_count_keeps_the_unchanged_colour()
    {
        // Act
        var foreground = FileRowAppearance.ForegroundFor(FileRowTone.Neutral, Color.White);

        // Assert
        Assert.Equal(Color.White, foreground);
    }
}
