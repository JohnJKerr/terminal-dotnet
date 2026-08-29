using TerminalDotnet.Explorer;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests;

public class WhenCreatingATestPanelSnapshot
{
    [Fact]
    public void It_exposes_execution_output_as_scrollable_lines()
    {
        // Arrange
        var state = new ExplorerState(
            ExplorerStatus.Ready,
            [],
            0,
            "first line\nsecond line");

        // Act
        var snapshot = TestPanelSnapshot.From(state, "Example.slnx");

        // Assert
        Assert.Equal(["first line", "second line"], snapshot.OutputLines);
    }

    [Fact]
    public void It_preserves_the_explorer_selection()
    {
        // Arrange
        var state = new ExplorerState(
            ExplorerStatus.Ready,
            [],
            4,
            "Ready");

        // Act
        var snapshot = TestPanelSnapshot.From(state, "Example.slnx");

        // Assert
        Assert.Equal(4, snapshot.SelectedIndex);
    }
}
