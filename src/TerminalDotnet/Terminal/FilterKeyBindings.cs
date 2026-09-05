using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using TerminalDotnet.Filters;

namespace TerminalDotnet.Terminal;

public static class FilterKeyBindings
{
    public static ExplorerFilter? FilterFor(Key key)
    {
        var code = (int)key.NoShift.KeyCode;
        return code >= (int)KeyCode.D1 && code <= (int)KeyCode.D9
            ? PanelFilters.Numbered(code - (int)KeyCode.D1 + 1)
            : null;
    }
}
