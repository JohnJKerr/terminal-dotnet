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
    public sealed record SelectPanel(PanelKind Panel) : ShellAction;
    public sealed record SelectFocusedPanel : ShellAction;
    public sealed record ShowCommands : ShellAction;
    public sealed record Quit : ShellAction;
}

public static class ShellKeyBindings
{
    public static ShellAction? ActionFor(
        Key key,
        bool searchFocused,
        bool panelsFocused,
        bool searchActive = false)
    {
        if (IsCtrl(key, KeyCode.K))
        {
            return new ShellAction.ShowCommands();
        }

        if (searchFocused)
        {
            return SearchActionFor(key);
        }

        if (PanelFor(key) is { } panel)
        {
            return new ShellAction.SelectPanel(panel);
        }

        if (searchActive && Is(key, KeyCode.Esc))
        {
            return new ShellAction.ClearSearch();
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

        return Is(key, KeyCode.Enter) ? new ShellAction.SelectFocusedPanel() : null;
    }

    private static PanelKind? PanelFor(Key key) => key.IsShift
        ? key.NoShift.KeyCode switch
        {
            KeyCode.E => PanelKind.Explorer,
            KeyCode.T => PanelKind.Tests,
            KeyCode.C => PanelKind.Changes,
            _ => null
        }
        : null;

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

    private static bool Is(Key key, KeyCode keyCode) =>
        !key.IsShift && key.NoShift.KeyCode == keyCode;

    private static bool IsCtrl(Key key, KeyCode keyCode) =>
        key.IsCtrl && key.NoShift.NoCtrl.NoAlt.KeyCode == keyCode;

}
