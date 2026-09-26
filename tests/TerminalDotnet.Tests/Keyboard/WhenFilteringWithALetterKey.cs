using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using TerminalDotnet.Filters;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Keyboard;

public sealed class WhenFilteringWithALetterKey
{
    [Fact]
    public void Pressing_capital_U_picks_the_updated_filter()
    {
        // Act
        var filter = FilterKeyBindings.FilterFor(new Key(KeyCode.U | KeyCode.ShiftMask));

        // Assert
        Assert.Equal(ExplorerFilter.Updated, filter);
    }

    [Fact]
    public void Pressing_a_lowercase_u_picks_no_filter()
    {
        // Act
        var filter = FilterKeyBindings.FilterFor(new Key(KeyCode.U));

        // Assert
        Assert.Null(filter);
    }

    [Fact]
    public void Pressing_1_picks_no_filter()
    {
        // Act
        var filter = FilterKeyBindings.FilterFor(new Key(KeyCode.D1));

        // Assert
        Assert.Null(filter);
    }

    [Fact]
    public void Pressing_a_capital_the_explorer_does_not_offer_picks_no_filter()
    {
        // Act
        var filter = FilterKeyBindings.FilterFor(new Key(KeyCode.F | KeyCode.ShiftMask));

        // Assert
        Assert.Null(filter);
    }
}
