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
        Assert.Equal(["1. Updated"], snapshot.Filters.Select(chip => chip.Text));
    }

    [Fact]
    public void The_explorer_marks_the_filter_it_is_using()
    {
        // Arrange
        var state = new FileExplorerState([], ActiveFilter: ExplorerFilter.Updated);

        // Act
        var snapshot = FilePanelSnapshot.From(state);

        // Assert
        Assert.True(snapshot.Filters.Single().IsActive);
    }

    [Fact]
    public void The_test_panel_offers_the_updated_filter_under_the_search()
    {
        // Arrange
        var state = new ExplorerState(ExplorerStatus.Ready, [], 0, "Ready");

        // Act
        var snapshot = TestPanelSnapshot.From(state, "App.slnx");

        // Assert
        Assert.Equal(["1. Updated"], snapshot.Filters.Select(chip => chip.Text));
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
        Assert.True(snapshot.Filters.Single().IsActive);
    }

    [Fact]
    public void An_active_filter_stands_out_from_the_ones_it_offers()
    {
        // Act
        var foreground = FilterAppearance.ForegroundFor(isActive: true);

        // Assert
        Assert.Equal(global::Terminal.Gui.Drawing.Color.BrightGreen, foreground);
    }

    [Fact]
    public void An_unused_filter_stays_muted()
    {
        // Act
        var foreground = FilterAppearance.ForegroundFor(isActive: false);

        // Assert
        Assert.Equal(global::Terminal.Gui.Drawing.Color.Gray, foreground);
    }
}
