using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Terminal;

public sealed class WhenMovingBetweenPanels
{
    [Fact]
    public void It_moves_to_the_panel_below()
    {
        // Arrange
        var shell = new PanelShell();

        // Act
        shell.SelectNext();

        // Assert
        Assert.Equal(PanelKind.Tests, shell.State.ActivePanel);
    }

    [Fact]
    public void It_moves_to_the_panel_above()
    {
        // Arrange
        var shell = new PanelShell();
        shell.Select(2);

        // Act
        shell.SelectPrevious();

        // Assert
        Assert.Equal(PanelKind.Tests, shell.State.ActivePanel);
    }

    [Fact]
    public void It_wraps_to_the_first_panel_from_the_last()
    {
        // Arrange
        var shell = new PanelShell();
        shell.Select(2);

        // Act
        shell.SelectNext();

        // Assert
        Assert.Equal(PanelKind.Explorer, shell.State.ActivePanel);
    }

    [Fact]
    public void It_wraps_to_the_last_panel_from_the_first()
    {
        // Arrange
        var shell = new PanelShell();

        // Act
        shell.SelectPrevious();

        // Assert
        Assert.Equal(PanelKind.Changes, shell.State.ActivePanel);
    }

    [Fact]
    public void Pressing_alt_up_moves_to_the_panel_above()
    {
        // Act
        var action = ShellKeyBindings.ActionFor(new Key(KeyCode.CursorUp).WithAlt, false, false);

        // Assert
        Assert.Equal(new ShellAction.PreviousPanel(), action);
    }

    [Fact]
    public void Pressing_alt_down_moves_to_the_panel_below()
    {
        // Act
        var action = ShellKeyBindings.ActionFor(new Key(KeyCode.CursorDown).WithAlt, false, false);

        // Assert
        Assert.Equal(new ShellAction.NextPanel(), action);
    }

    [Fact]
    public void Pressing_alt_down_in_the_search_still_moves_between_panels()
    {
        // Act
        var action = ShellKeyBindings.ActionFor(new Key(KeyCode.CursorDown).WithAlt, true, false);

        // Assert
        Assert.Equal(new ShellAction.NextPanel(), action);
    }

    [Fact]
    public void Pressing_down_on_its_own_still_moves_the_selection()
    {
        // Act
        var action = ShellKeyBindings.ActionFor(new Key(KeyCode.CursorDown), false, false);

        // Assert
        Assert.Null(action);
    }
}
