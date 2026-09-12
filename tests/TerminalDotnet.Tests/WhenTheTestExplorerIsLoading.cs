using TerminalDotnet.Explorer;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Terminal;

public sealed class WhenTheTestExplorerIsLoading
{
    private static readonly ExplorerState Loading =
        new(ExplorerStatus.Loading, [], 0, "Discovering tests...");

    [Fact]
    public void It_says_what_it_is_waiting_for_where_the_tree_will_be()
    {
        // Act
        var snapshot = TestPanelSnapshot.From(Loading, "Shop.slnx", TimeSpan.Zero);

        // Assert
        Assert.Equal(
            $"{ActivityMarker.MarkerAt(TimeSpan.Zero)} Discovering tests...",
            snapshot.EmptyMessage);
    }

    [Fact]
    public void It_turns_the_marker_as_discovery_goes_on()
    {
        // Act
        var later = TestPanelSnapshot.From(Loading, "Shop.slnx", ActivityMarker.FrameDuration);

        // Assert
        Assert.NotEqual(
            TestPanelSnapshot.From(Loading, "Shop.slnx", TimeSpan.Zero).EmptyMessage,
            later.EmptyMessage);
    }

    [Fact]
    public void It_says_there_are_no_tests_once_discovery_has_found_none()
    {
        // Arrange
        var state = new ExplorerState(ExplorerStatus.Ready, [], 0, "Ready — 0 tests discovered");

        // Act
        var snapshot = TestPanelSnapshot.From(state, "Shop.slnx");

        // Assert
        Assert.Equal("No tests to show", snapshot.EmptyMessage);
    }

    [Fact]
    public void It_leaves_the_status_line_unmarked()
    {
        // Act
        var snapshot = TestPanelSnapshot.From(Loading, "Shop.slnx", ActivityMarker.FrameDuration);

        // Assert
        Assert.Equal("Discovering tests...", snapshot.StatusLine);
    }

    [Fact]
    public void It_leaves_a_running_status_line_to_the_inline_markers()
    {
        // Arrange
        var state = new ExplorerState(ExplorerStatus.Running, [], 0, "Running 3 tests...");

        // Act
        var snapshot = TestPanelSnapshot.From(state, "Shop.slnx", ActivityMarker.FrameDuration);

        // Assert
        Assert.Equal("Running 3 tests...", snapshot.StatusLine);
    }
}
