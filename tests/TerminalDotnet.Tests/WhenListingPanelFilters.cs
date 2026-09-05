using TerminalDotnet.Filters;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Explorer;

public sealed class WhenListingPanelFilters
{
    [Fact]
    public void It_numbers_the_updated_filter_first()
    {
        // Act
        var chips = PanelFilters.Chips(active: null);

        // Assert
        Assert.Equal(["1. Updated"], chips.Select(chip => chip.Text));
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
        var filter = PanelFilters.Numbered(9);

        // Assert
        Assert.Null(filter);
    }
}
