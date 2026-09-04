using Terminal.Gui.Drivers;
using Terminal.Gui.Input;

namespace TerminalDotnet.Terminal;

public abstract record ShellAction
{
    public sealed record TypeIntoSearch : ShellAction;
    public sealed record ClearSearch : ShellAction;
    public sealed record LeaveSearch : ShellAction;
    public sealed record FocusSearch : ShellAction;
    public sealed record FocusPanels : ShellAction;
    public sealed record FocusRows : ShellAction;
    public sealed record SelectPanel : ShellAction;
    public sealed record Quit : ShellAction;
}

public static class ShellKeyBindings
{
    public static ShellAction? ActionFor(Key key, bool searchFocused, bool panelsFocused)
    {
        if (searchFocused)
        {
            return SearchActionFor(key);
        }

        if (Is(key, KeyCode.Q) || Is(key, KeyCode.Esc))
        {
            return new ShellAction.Quit();
        }

        if (Is(key, KeyCode.S))
        {
            return new ShellAction.FocusSearch();
        }

        if (Is(key, KeyCode.CursorLeft))
        {
            return panelsFocused ? null : new ShellAction.FocusPanels();
        }

        if (!panelsFocused)
        {
            return null;
        }

        if (Is(key, KeyCode.CursorRight))
        {
            return new ShellAction.FocusRows();
        }

        return Is(key, KeyCode.Enter) ? new ShellAction.SelectPanel() : null;
    }

    private static ShellAction SearchActionFor(Key key)
    {
        if (Is(key, KeyCode.Esc))
        {
            return new ShellAction.ClearSearch();
        }

        return Is(key, KeyCode.Enter)
            ? new ShellAction.LeaveSearch()
            : new ShellAction.TypeIntoSearch();
    }

    private static bool Is(Key key, KeyCode keyCode) => key.NoShift.KeyCode == keyCode;
}
