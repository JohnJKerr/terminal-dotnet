using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using TerminalDotnet.Filters;

namespace TerminalDotnet.Terminal;

public static class FilterKeyBindings
{
    public static ExplorerFilter? FilterFor(Key key) => FilterFor(key, PanelFilters.Lettered);

    public static ExplorerFilter? TestFilterFor(Key key) => FilterFor(key, PanelFilters.LetteredTest);

    private static ExplorerFilter? FilterFor(Key key, Func<string, ExplorerFilter?> lettered) =>
        key.IsShift ? lettered(((char)key.NoShift.KeyCode).ToString()) : null;
}
