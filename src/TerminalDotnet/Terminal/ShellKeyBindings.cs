using Terminal.Gui.Drivers;
using Terminal.Gui.Input;

namespace TerminalDotnet.Terminal;

public abstract record ShellAction
{
    public sealed record TypeIntoSearch : ShellAction;
    public sealed record ClearSearch : ShellAction;
    public sealed record LeaveSearch : ShellAction;
    public sealed record FocusSearch : ShellAction;
    public sealed record SelectPanel(PanelKind Panel) : ShellAction;
    public sealed record SelectNextPanel : ShellAction;
    public sealed record SelectPreviousPanel : ShellAction;
    public sealed record ShowCommands : ShellAction;
    public sealed record Refresh : ShellAction;
    public sealed record Quit : ShellAction;

    /// <summary>Escape with nothing left to close. It is taken so that it
    /// cannot reach the terminal framework, which would quit on it.</summary>
    public sealed record Dismiss : ShellAction;

    /// <summary>A sideways arrow. Panels are reached by number and Tab, so
    /// the arrows are kept from the terminal framework, which would otherwise
    /// move the focus behind the shell's back.</summary>
    public sealed record HoldFocus : ShellAction;
}

public static class ShellKeyBindings
{
    public static ShellAction? ActionFor(
        Key key,
        bool searchFocused,
        bool searchActive = false)
    {
        if (Is(key, (KeyCode)'?'))
        {
            return new ShellAction.ShowCommands();
        }

        // The refresh is the workspace's, not one panel's, so it answers from
        // wherever the reader is standing and through the search box, which
        // every bare letter would otherwise be typed into.
        if (IsCtrl(key, KeyCode.R))
        {
            return new ShellAction.Refresh();
        }

        if (searchFocused)
        {
            return SearchActionFor(key);
        }

        if (PanelFor(key) is { } panel)
        {
            return new ShellAction.SelectPanel(panel);
        }

        if (key.NoShift.KeyCode == KeyCode.Tab)
        {
            return key.IsShift ? new ShellAction.SelectPreviousPanel() : new ShellAction.SelectNextPanel();
        }

        if (Is(key, KeyCode.Esc))
        {
            return searchActive ? new ShellAction.ClearSearch() : new ShellAction.Dismiss();
        }

        if (Is(key, KeyCode.Q))
        {
            return new ShellAction.Quit();
        }

        if (Is(key, KeyCode.CursorLeft) || Is(key, KeyCode.CursorRight))
        {
            return new ShellAction.HoldFocus();
        }

        return Is(key, (KeyCode)'/') ? new ShellAction.FocusSearch() : null;
    }

    private static PanelKind? PanelFor(Key key) =>
        key.IsShift || key.IsCtrl || key.IsAlt ? null : PanelKeys.For(((char)key.KeyCode).ToString());

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
