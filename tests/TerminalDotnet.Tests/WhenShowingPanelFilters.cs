using TerminalDotnet.Explorer;
using TerminalDotnet.Files;
using TerminalDotnet.Filters;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Terminal;

public sealed class WhenShowingPanelFilters
{
    [Fact]
    public void The_explorer_offers_the_updated_filter_under_the_search()
    {
        // Arrange
        var state = new FileExplorerState([]);

        // Act
        var snapshot = FilePanelSnapshot.From(state);

        // Assert
        Assert.Equal(["A All files", "U Updated"], snapshot.Filters.Select(chip => chip.Text));
    }

    [Fact]
    public void The_explorer_marks_the_filter_it_is_using()
    {
        // Arrange
        var state = new FileExplorerState([], ActiveFilter: ExplorerFilter.Updated);

        // Act
        var snapshot = FilePanelSnapshot.From(state);

        // Assert
        Assert.True(snapshot.Filters[1].IsActive);
    }

    [Fact]
    public void The_explorer_marks_every_file_while_it_shows_them()
    {
        // Arrange
        var state = new FileExplorerState([]);

        // Act
        var snapshot = FilePanelSnapshot.From(state, showsAllFiles: true);

        // Assert
        Assert.True(snapshot.Filters[0].IsActive);
    }

    [Fact]
    public void The_test_panel_offers_its_filters_under_the_search()
    {
        // Arrange
        var state = new ExplorerState(ExplorerStatus.Ready, [], 0, "Ready");

        // Act
        var snapshot = TestPanelSnapshot.From(state, "App.slnx");

        // Assert
        Assert.Equal(
            ["U Updated", "F Failing", "P Passing", "L Last run", "N Not run"],
            snapshot.Filters.Select(chip => chip.Text));
    }

    [Fact]
    public void The_test_panel_marks_the_filter_it_is_using()
    {
        // Arrange
        var state = new ExplorerState(
            ExplorerStatus.Ready,
            [],
            0,
            "Ready",
            ActiveFilter: ExplorerFilter.Updated);

        // Act
        var snapshot = TestPanelSnapshot.From(state, "App.slnx");

        // Assert
        Assert.True(snapshot.Filters.Single(chip => chip.IsActive).Text == "U Updated");
    }

}
