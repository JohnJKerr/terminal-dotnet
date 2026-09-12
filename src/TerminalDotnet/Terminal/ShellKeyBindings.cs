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
    public sealed record ShowCommands : ShellAction;
    public sealed record PreviousPanel : ShellAction;
    public sealed record NextPanel : ShellAction;
    public sealed record AwaitPanelTarget : ShellAction;
    public sealed record SelectNumberedPanel(int Number) : ShellAction;
    public sealed record Quit : ShellAction;
}

public static class ShellKeyBindings
{
    public static ShellAction? ActionFor(
        Key key,
        bool searchFocused,
        bool panelsFocused,
        bool awaitingPanelTarget = false)
    {
        if (IsCtrl(key, KeyCode.K))
        {
            return new ShellAction.ShowCommands();
        }

        if (searchFocused)
        {
            return SearchActionFor(key);
        }

        if (awaitingPanelTarget && PanelActionFor(key) is { } panelAction)
        {
            return panelAction;
        }

        if (Is(key, KeyCode.G))
        {
            return new ShellAction.AwaitPanelTarget();
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

    private static bool IsCtrl(Key key, KeyCode keyCode) =>
        key.IsCtrl && key.NoShift.NoCtrl.NoAlt.KeyCode == keyCode;

    /// <summary>
    /// The panel a "g" is waiting on: an arrow to step between panels, or the
    /// number a panel carries. A key that names neither gives up the wait and
    /// is handled as it would have been on its own.
    /// </summary>
    private static ShellAction? PanelActionFor(Key key)
    {
        if (Is(key, KeyCode.CursorUp))
        {
            return new ShellAction.PreviousPanel();
        }

        if (Is(key, KeyCode.CursorDown))
        {
            return new ShellAction.NextPanel();
        }

        return PanelNumber(key) is { } number
            ? new ShellAction.SelectNumberedPanel(number)
            : null;
    }

    private static int? PanelNumber(Key key)
    {
        var code = (int)key.NoShift.KeyCode;
        return code >= (int)KeyCode.D1 && code <= (int)KeyCode.D9
            ? code - (int)KeyCode.D1 + 1
            : null;
    }
}
