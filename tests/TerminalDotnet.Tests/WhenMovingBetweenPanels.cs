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
        shell.Select(4);

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
        Assert.Equal(PanelKind.Comments, shell.State.ActivePanel);
    }

    [Fact]
    public void Pressing_1_goes_to_the_explorer()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.D1));

        // Assert
        Assert.Equal(new ShellAction.SelectPanel(PanelKind.Explorer), action);
    }

    [Fact]
    public void Pressing_2_goes_to_the_tests()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.D2));

        // Assert
        Assert.Equal(new ShellAction.SelectPanel(PanelKind.Tests), action);
    }

    [Fact]
    public void Pressing_3_goes_to_the_changes()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.D3));

        // Assert
        Assert.Equal(new ShellAction.SelectPanel(PanelKind.Changes), action);
    }

    [Fact]
    public void Pressing_4_goes_to_the_issues()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.D4));

        // Assert
        Assert.Equal(new ShellAction.SelectPanel(PanelKind.Issues), action);
    }

    [Fact]
    public void Pressing_5_goes_to_the_comments()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.D5));

        // Assert
        Assert.Equal(new ShellAction.SelectPanel(PanelKind.Comments), action);
    }

    [Fact]
    public void Pressing_capital_E_goes_to_no_panel()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.E | KeyCode.ShiftMask));

        // Assert
        Assert.Null(action);
    }

    [Fact]
    public void Pressing_6_goes_to_no_panel()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.D6));

        // Assert
        Assert.Null(action);
    }

    [Fact]
    public void Pressing_lowercase_e_leaves_the_panel_alone()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.E));

        // Assert
        Assert.Null(action);
    }

    [Fact]
    public void Pressing_lowercase_f_leaves_the_panel_alone()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.F));

        // Assert
        Assert.Null(action);
    }

    [Fact]
    public void Pressing_lowercase_c_leaves_the_panel_alone()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.C));

        // Assert
        Assert.Null(action);
    }

    [Fact]
    public void Pressing_2_in_the_search_types_it()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.D2), searchFocused: true);

        // Assert
        Assert.Equal(new ShellAction.TypeIntoSearch(), action);
    }

    [Fact]
    public void Pressing_capital_Q_does_not_quit()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.Q | KeyCode.ShiftMask));

        // Assert
        Assert.Null(action);
    }

    [Fact]
    public void Pressing_the_down_arrow_still_moves_the_selection()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.CursorDown));

        // Assert
        Assert.Null(action);
    }

    private static ShellAction? ActionFor(
        Key key,
        bool searchFocused = false,
        bool panelsFocused = false) =>
        ShellKeyBindings.ActionFor(key, searchFocused, panelsFocused);
}
