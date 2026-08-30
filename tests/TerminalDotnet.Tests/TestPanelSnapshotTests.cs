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
        Assert.Equal(["first line", "second line"], snapshot.OutputLines.Select(line => line.Text));
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

    [Fact]
    public void It_exposes_the_active_test_search()
    {
        // Arrange
        var state = new ExplorerState(
            ExplorerStatus.Ready,
            [],
            0,
            "Ready",
            SearchQuery: "cart");

        // Act
        var snapshot = TestPanelSnapshot.From(state, "Example.slnx");

        // Assert
        Assert.Equal("cart", snapshot.SearchQuery);
    }

    [Fact]
    public void It_marks_failed_output_as_failure()
    {
        // Arrange
        var state = new ExplorerState(
            ExplorerStatus.Failed,
            [],
            0,
            "Failed! - Failed: 1, Passed: 0");

        // Act
        var snapshot = TestPanelSnapshot.From(state, "Example.slnx");

        // Assert
        Assert.Equal(OutputLineTone.Failure, snapshot.OutputLines[0].Tone);
    }

    [Theory]
    [InlineData("Passed! - Passed: 12", OutputLineTone.Success)]
    [InlineData("Skipped: 2", OutputLineTone.Skipped)]
    [InlineData("Running 12 tests...", OutputLineTone.Status)]
    public void It_marks_test_status_output_with_its_tone(string output, OutputLineTone expectedTone)
    {
        // Arrange
        var state = new ExplorerState(
            ExplorerStatus.Ready,
            [],
            0,
            output);

        // Act
        var snapshot = TestPanelSnapshot.From(state, "Example.slnx");

        // Assert
        Assert.Equal(expectedTone, snapshot.OutputLines[0].Tone);
    }
}
