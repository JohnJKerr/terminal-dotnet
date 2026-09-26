using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using TerminalDotnet.Files;
using TerminalDotnet.Filters;
using static TerminalDotnet.Terminal.KeyMatch;

namespace TerminalDotnet.Terminal;

public abstract record FilePanelAction
{
    public sealed record OpenFile(string Path) : FilePanelAction;
    public sealed record ToggleFilter(ExplorerFilter Filter) : FilePanelAction;
    public sealed record ToggleAllFiles : FilePanelAction;
}

public static class FilePanelKeyBindings
{
    public static FilePanelAction? ActionFor(
        Key key,
        VisibleFileNode? selected,
        bool searchActive)
    {
        if (searchActive)
        {
            return null;
        }

        if (FilterKeyBindings.FilterFor(key) is { } filter)
        {
            return new FilePanelAction.ToggleFilter(filter);
        }

        if (key.IsShift && key.NoShift.KeyCode == KeyCode.A)
        {
            return new FilePanelAction.ToggleAllFiles();
        }

        if (selected is null || selected.Kind != FileNodeKind.File)
        {
            return null;
        }

        return Is(key, KeyCode.Enter) || Is(key, KeyCode.E)
            ? new FilePanelAction.OpenFile(selected.Files[0].Path)
            : null;
    }
}
