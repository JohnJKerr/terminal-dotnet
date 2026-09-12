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
    public void It_numbers_every_panel()
    {
        // Arrange
        var shell = new PanelShell();

        // Act
        var numbered = shell.State.NumberedPanels;

        // Assert
        Assert.Equal(["1. Explorer", "2. Tests", "3. Changes"], numbered);
    }

    [Fact]
    public void It_targets_a_panel_by_its_number()
    {
        // Arrange
        var shell = new PanelShell();

        // Act
        shell.SelectNumbered(3);

        // Assert
        Assert.Equal(PanelKind.Changes, shell.State.ActivePanel);
    }

    [Fact]
    public void It_stays_put_for_a_number_no_panel_has()
    {
        // Arrange
        var shell = new PanelShell();
        shell.SelectNumbered(2);

        // Act
        shell.SelectNumbered(9);

        // Assert
        Assert.Equal(PanelKind.Tests, shell.State.ActivePanel);
    }

    [Fact]
    public void Pressing_alt_1_targets_the_first_panel()
    {
        // Act
        var action = ShellKeyBindings.ActionFor(new Key(KeyCode.D1).WithAlt, false, false);

        // Assert
        Assert.Equal(new ShellAction.SelectNumberedPanel(1), action);
    }

    [Fact]
    public void Pressing_alt_3_targets_the_third_panel()
    {
        // Act
        var action = ShellKeyBindings.ActionFor(new Key(KeyCode.D3).WithAlt, false, false);

        // Assert
        Assert.Equal(new ShellAction.SelectNumberedPanel(3), action);
    }

    [Fact]
    public void Pressing_alt_2_in_the_search_still_targets_a_panel()
    {
        // Act
        var action = ShellKeyBindings.ActionFor(new Key(KeyCode.D2).WithAlt, true, false);

        // Assert
        Assert.Equal(new ShellAction.SelectNumberedPanel(2), action);
    }

    [Fact]
    public void Pressing_a_number_on_its_own_still_reaches_the_panel_filters()
    {
        // Act
        var action = ShellKeyBindings.ActionFor(new Key(KeyCode.D1), false, false);

        // Assert
        Assert.Null(action);
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
