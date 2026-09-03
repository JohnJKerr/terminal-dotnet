using Terminal.Gui.Drawing;
using TerminalDotnet.Explorer;
using TerminalDotnet.Terminal;
using TerminalDotnet.Testing;
using Xunit;

namespace TerminalDotnet.Tests.Testing;

public sealed class WhenStylingTestStatus
{
    [Theory]
    [InlineData(ExplorerStatus.Failed, false, Color.BrightRed)]
    [InlineData(ExplorerStatus.Running, false, Color.BrightCyan)]
    [InlineData(ExplorerStatus.Ready, true, Color.BrightGreen)]
    [InlineData(ExplorerStatus.Ready, false, Color.White)]
    public void It_colors_the_static_status_line(
        ExplorerStatus status,
        bool passed,
        Color expected)
    {
        // Arrange
        var state = new ExplorerState(status, [], 0, "Status", new TestRun(passed, "Status"));

        // Act
        var foreground = TestStatusAppearance.ForegroundFor(state);

        // Assert
        Assert.Equal(expected, foreground);
    }
}
