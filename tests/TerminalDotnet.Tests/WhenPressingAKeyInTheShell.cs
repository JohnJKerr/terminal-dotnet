using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Terminal;

public sealed class WhenPressingAKeyInTheShell
{
    [Fact]
    public void Pressing_q_quits()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.Q));

        // Assert
        Assert.Equal(new ShellAction.Quit(), action);
    }

    [Fact]
    public void Pressing_s_moves_to_the_search()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.S));

        // Assert
        Assert.Equal(new ShellAction.FocusSearch(), action);
    }

    [Fact]
    public void Pressing_the_left_arrow_moves_to_the_panels()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.CursorLeft));

        // Assert
        Assert.Equal(new ShellAction.FocusPanels(), action);
    }

    [Fact]
    public void Pressing_the_right_arrow_in_the_panels_moves_to_the_rows()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.CursorRight), panelsFocused: true);

        // Assert
        Assert.Equal(new ShellAction.FocusRows(), action);
    }

    [Fact]
    public void Pressing_enter_in_the_panels_selects_the_panel()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.Enter), panelsFocused: true);

        // Assert
        Assert.Equal(new ShellAction.SelectFocusedPanel(), action);
    }

    [Fact]
    public void Pressing_enter_in_the_rows_leaves_the_key_to_the_panel()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.Enter));

        // Assert
        Assert.Null(action);
    }

    [Fact]
    public void Pressing_escape_in_the_search_clears_it()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.Esc), searchFocused: true);

        // Assert
        Assert.Equal(new ShellAction.ClearSearch(), action);
    }

    [Fact]
    public void Pressing_enter_in_the_search_leaves_it()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.Enter), searchFocused: true);

        // Assert
        Assert.Equal(new ShellAction.LeaveSearch(), action);
    }

    [Fact]
    public void Pressing_q_in_the_search_types_it()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.Q), searchFocused: true);

        // Assert
        Assert.Equal(new ShellAction.TypeIntoSearch(), action);
    }

    [Fact]
    public void Pressing_question_mark_shows_every_command()
    {
        // Act
        var action = ActionFor(new Key((KeyCode)'?'));

        // Assert
        Assert.Equal(new ShellAction.ShowCommands(), action);
    }

    [Fact]
    public void Pressing_question_mark_in_the_search_shows_every_command()
    {
        // Act
        var action = ActionFor(new Key((KeyCode)'?'), searchFocused: true);

        // Assert
        Assert.Equal(new ShellAction.ShowCommands(), action);
    }

    [Fact]
    public void Pressing_ctrl_k_no_longer_opens_the_command_list()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.K).WithCtrl);

        // Assert
        Assert.Null(action);
    }

    [Fact]
    public void Pressing_k_on_its_own_still_moves_the_selection()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.K));

        // Assert
        Assert.Null(action);
    }

    [Fact]
    public void Pressing_escape_in_the_rows_clears_a_search_that_is_running()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.Esc), searchActive: true);

        // Assert
        Assert.Equal(new ShellAction.ClearSearch(), action);
    }

    [Fact]
    public void Pressing_escape_in_the_rows_is_swallowed_when_nothing_is_searched()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.Esc));

        // Assert
        Assert.Equal(new ShellAction.Dismiss(), action);
    }

    [Fact]
    public void Pressing_q_still_quits_while_a_search_is_running()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.Q), searchActive: true);

        // Assert
        Assert.Equal(new ShellAction.Quit(), action);
    }

    private static ShellAction? ActionFor(
        Key key,
        bool searchFocused = false,
        bool panelsFocused = false,
        bool searchActive = false) =>
        ShellKeyBindings.ActionFor(key, searchFocused, panelsFocused, searchActive);
}
