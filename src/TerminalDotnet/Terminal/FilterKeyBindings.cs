using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using TerminalDotnet.Filters;

namespace TerminalDotnet.Terminal;

public static class FilterKeyBindings
{
    public static ExplorerFilter? FilterFor(Key key) => key.IsShift
        ? PanelFilters.Lettered(((char)key.NoShift.KeyCode).ToString())
        : null;

    public static ExplorerFilter? TestFilterFor(Key key)
        => FilterFor(key, PanelFilters.NumberedTest);

    private static ExplorerFilter? FilterFor(
        Key key,
        Func<int, ExplorerFilter?> numbered)
    {
        if (key.IsShift)
        {
            return null;
        }

        var code = (int)key.NoShift.KeyCode;
        return code >= (int)KeyCode.D1 && code <= (int)KeyCode.D9
            ? numbered(code - (int)KeyCode.D1 + 1)
            : null;
    }
}
