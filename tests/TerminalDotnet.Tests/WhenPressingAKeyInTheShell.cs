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
    public void Pressing_escape_quits()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.Esc));

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
        Assert.Equal(new ShellAction.SelectPanel(), action);
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

    private static ShellAction? ActionFor(
        Key key,
        bool searchFocused = false,
        bool panelsFocused = false) =>
        ShellKeyBindings.ActionFor(key, searchFocused, panelsFocused);
}
