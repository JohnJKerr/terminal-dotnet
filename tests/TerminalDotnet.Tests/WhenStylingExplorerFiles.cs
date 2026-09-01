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
}
