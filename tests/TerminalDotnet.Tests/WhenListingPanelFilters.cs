using TerminalDotnet.Filters;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Explorer;

public sealed class WhenListingPanelFilters
{
    [Fact]
    public void It_names_the_updated_filter_by_its_key()
    {
        // Act
        var chips = PanelFilters.Chips(active: null);

        // Assert
        Assert.Equal(["U Updated"], chips.Select(chip => chip.Text));
    }

    [Fact]
    public void It_marks_the_filter_the_panel_is_using()
    {
        // Act
        var chips = PanelFilters.Chips(ExplorerFilter.Updated);

        // Assert
        Assert.True(chips.Single().IsActive);
    }

    [Fact]
    public void It_leaves_an_unused_filter_unmarked()
    {
        // Act
        var chips = PanelFilters.Chips(active: null);

        // Assert
        Assert.False(chips.Single().IsActive);
    }

    [Fact]
    public void It_has_no_filter_beyond_the_ones_it_offers()
    {
        // Act
        var filter = PanelFilters.Lettered("F");

        // Assert
        Assert.Null(filter);
    }

    [Fact]
    public void The_test_panel_names_its_run_filters_after_updated()
    {
        // Act
        var chips = PanelFilters.TestChips(active: null);

        // Assert
        Assert.Equal(
            ["U Updated", "F Failing", "P Passing", "L Last run", "N Not run"],
            chips.Select(chip => chip.Text));
    }
}
