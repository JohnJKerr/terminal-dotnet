using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using TerminalDotnet.Filters;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Explorer;

public sealed class WhenFilteringWithANumberKey
{
    [Fact]
    public void Pressing_1_picks_the_updated_filter()
    {
        // Act
        var filter = FilterKeyBindings.FilterFor(new Key(KeyCode.D1));

        // Assert
        Assert.Equal(ExplorerFilter.Updated, filter);
    }

    [Fact]
    public void Pressing_2_picks_no_filter_while_only_one_is_offered()
    {
        // Act
        var filter = FilterKeyBindings.FilterFor(new Key(KeyCode.D2));

        // Assert
        Assert.Null(filter);
    }

    [Fact]
    public void Pressing_a_letter_picks_no_filter()
    {
        // Act
        var filter = FilterKeyBindings.FilterFor(new Key(KeyCode.J));

        // Assert
        Assert.Null(filter);
    }
}
