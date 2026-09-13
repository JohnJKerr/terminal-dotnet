using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using TerminalDotnet.Flags;

namespace TerminalDotnet.Terminal;

public abstract record FlagPanelAction
{
    public sealed record Edit(string Path, int Line) : FlagPanelAction;
    public sealed record Preview(string Path, int Line) : FlagPanelAction;
    public sealed record ToggleFilter(FlagCategory Category) : FlagPanelAction;
}

public static class FlagPanelKeyBindings
{
    public static FlagPanelAction? ActionFor(Key key, Flag? selected, bool searchActive)
    {
        if (searchActive)
        {
            return null;
        }
        if (NumberOf(key) is { } number && FlagFilters.Numbered(number) is { } filter)
        {
            return new FlagPanelAction.ToggleFilter(filter);
        }
        if (selected is null)
        {
            return null;
        }
        if (Is(key, KeyCode.Enter) || Is(key, KeyCode.E))
        {
            return new FlagPanelAction.Edit(selected.Path, selected.Line);
        }
        return Is(key, KeyCode.P) ? new FlagPanelAction.Preview(selected.Path, selected.Line) : null;
    }

    private static int? NumberOf(Key key) => key.KeyCode switch
    {
        KeyCode.D1 => 1, KeyCode.D2 => 2, KeyCode.D3 => 3, KeyCode.D4 => 4, _ => null
    };

    private static bool Is(Key key, KeyCode keyCode) => !key.IsShift && key.NoShift.KeyCode == keyCode;
}
