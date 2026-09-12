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
    public void Pressing_g_waits_for_a_panel_to_be_named()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.G));

        // Assert
        Assert.Equal(new ShellAction.AwaitPanelTarget(), action);
    }

    [Fact]
    public void Pressing_g_then_1_targets_the_first_panel()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.D1), awaitingPanelTarget: true);

        // Assert
        Assert.Equal(new ShellAction.SelectNumberedPanel(1), action);
    }

    [Fact]
    public void Pressing_g_then_3_targets_the_third_panel()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.D3), awaitingPanelTarget: true);

        // Assert
        Assert.Equal(new ShellAction.SelectNumberedPanel(3), action);
    }

    [Fact]
    public void Pressing_g_then_the_down_arrow_moves_to_the_panel_below()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.CursorDown), awaitingPanelTarget: true);

        // Assert
        Assert.Equal(new ShellAction.NextPanel(), action);
    }

    [Fact]
    public void Pressing_g_then_the_up_arrow_moves_to_the_panel_above()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.CursorUp), awaitingPanelTarget: true);

        // Assert
        Assert.Equal(new ShellAction.PreviousPanel(), action);
    }

    [Fact]
    public void Pressing_g_then_an_unrelated_key_gives_up_the_wait()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.Q), awaitingPanelTarget: true);

        // Assert
        Assert.Equal(new ShellAction.Quit(), action);
    }

    [Fact]
    public void Pressing_g_in_the_search_types_it()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.G), searchFocused: true);

        // Assert
        Assert.Equal(new ShellAction.TypeIntoSearch(), action);
    }

    [Fact]
    public void Pressing_a_number_on_its_own_still_reaches_the_panel_filters()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.D1));

        // Assert
        Assert.Null(action);
    }

    [Fact]
    public void Pressing_the_down_arrow_on_its_own_still_moves_the_selection()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.CursorDown));

        // Assert
        Assert.Null(action);
    }

    [Fact]
    public void It_keeps_waiting_after_a_panel_is_named()
    {
        // Arrange
        var action = ActionFor(new Key(KeyCode.D1), awaitingPanelTarget: true);

        // Act
        var keepsWaiting = ShellKeyBindings.ContinuesNavigating(action!);

        // Assert
        Assert.True(keepsWaiting);
    }

    [Fact]
    public void It_keeps_waiting_after_stepping_to_the_next_panel()
    {
        // Arrange
        var action = ActionFor(new Key(KeyCode.CursorDown), awaitingPanelTarget: true);

        // Act
        var keepsWaiting = ShellKeyBindings.ContinuesNavigating(action!);

        // Assert
        Assert.True(keepsWaiting);
    }

    [Fact]
    public void It_keeps_waiting_when_g_repeats_under_a_held_key()
    {
        // Arrange
        var action = ActionFor(new Key(KeyCode.G), awaitingPanelTarget: true);

        // Act
        var keepsWaiting = ShellKeyBindings.ContinuesNavigating(action!);

        // Assert
        Assert.True(keepsWaiting);
    }

    [Fact]
    public void It_gives_up_waiting_once_a_key_names_nowhere()
    {
        // Arrange
        var action = ActionFor(new Key(KeyCode.Q), awaitingPanelTarget: true);

        // Act
        var keepsWaiting = ShellKeyBindings.ContinuesNavigating(action!);

        // Assert
        Assert.False(keepsWaiting);
    }

    [Fact]
    public void Pressing_escape_while_waiting_stops_navigating_rather_than_quitting()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.Esc), awaitingPanelTarget: true);

        // Assert
        Assert.Equal(new ShellAction.StopNavigating(), action);
    }

    [Fact]
    public void It_gives_up_waiting_once_navigating_has_stopped()
    {
        // Act
        var keepsWaiting = ShellKeyBindings.ContinuesNavigating(new ShellAction.StopNavigating());

        // Assert
        Assert.False(keepsWaiting);
    }

    private static ShellAction? ActionFor(
        Key key,
        bool searchFocused = false,
        bool panelsFocused = false,
        bool awaitingPanelTarget = false) =>
        ShellKeyBindings.ActionFor(key, searchFocused, panelsFocused, awaitingPanelTarget);
}
