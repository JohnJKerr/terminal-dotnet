using TerminalDotnet.Explorer;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Terminal;

public sealed class WhenATestRunIsInFlight
{
    private static readonly ExplorerState Running =
        new(ExplorerStatus.Running, [], 0, "Running 3 tests...");

    [Fact]
    public void It_marks_the_status_line_while_the_tests_run()
    {
        // Act
        var snapshot = TestPanelSnapshot.From(Running, "Shop.slnx", TimeSpan.Zero);

        // Assert
        Assert.Equal($"{RunActivity.MarkerAt(TimeSpan.Zero)} Running 3 tests...", snapshot.StatusLine);
    }

    [Fact]
    public void It_turns_the_marker_as_the_run_goes_on()
    {
        // Act
        var later = TestPanelSnapshot.From(Running, "Shop.slnx", RunActivity.FrameDuration);

        // Assert
        Assert.NotEqual(
            TestPanelSnapshot.From(Running, "Shop.slnx", TimeSpan.Zero).StatusLine,
            later.StatusLine);
    }

    [Fact]
    public void It_returns_to_the_first_frame_after_a_full_turn()
    {
        // Act
        var marker = RunActivity.MarkerAt(RunActivity.FrameDuration * RunActivity.FrameCount);

        // Assert
        Assert.Equal(RunActivity.MarkerAt(TimeSpan.Zero), marker);
    }

    [Fact]
    public void It_holds_each_frame_for_its_whole_turn()
    {
        // Act
        var marker = RunActivity.MarkerAt(RunActivity.FrameDuration - TimeSpan.FromTicks(1));

        // Assert
        Assert.Equal(RunActivity.MarkerAt(TimeSpan.Zero), marker);
    }

    [Fact]
    public void It_leaves_the_status_line_unmarked_once_the_run_has_finished()
    {
        // Arrange
        var state = new ExplorerState(ExplorerStatus.Ready, [], 0, "Ready — 12 tests discovered");

        // Act
        var snapshot = TestPanelSnapshot.From(state, "Shop.slnx", RunActivity.FrameDuration);

        // Assert
        Assert.Equal("Ready — 12 tests discovered", snapshot.StatusLine);
    }

    [Fact]
    public void It_leaves_the_status_line_unmarked_while_tests_are_still_being_discovered()
    {
        // Arrange
        var state = new ExplorerState(ExplorerStatus.Loading, [], 0, "Discovering tests...");

        // Act
        var snapshot = TestPanelSnapshot.From(state, "Shop.slnx", RunActivity.FrameDuration);

        // Assert
        Assert.Equal("Discovering tests...", snapshot.StatusLine);
    }
}
