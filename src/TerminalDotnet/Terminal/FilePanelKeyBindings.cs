using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using TerminalDotnet.Files;
using TerminalDotnet.Filters;

namespace TerminalDotnet.Terminal;

public abstract record FilePanelAction
{
    public sealed record OpenFile(string Path) : FilePanelAction;
    public sealed record PreviewFile(string Path) : FilePanelAction;
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

        if (Is(key, KeyCode.Enter) || Is(key, KeyCode.E))
        {
            return new FilePanelAction.OpenFile(selected.Files[0].Path);
        }

        return Is(key, KeyCode.P)
            ? new FilePanelAction.PreviewFile(selected.Files[0].Path)
            : null;
    }

    private static bool Is(Key key, KeyCode keyCode) =>
        !key.IsShift && key.NoShift.KeyCode == keyCode;
}
