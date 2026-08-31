using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using TerminalDotnet.Files;

namespace TerminalDotnet.Terminal;

public abstract record FilePanelAction
{
    public sealed record OpenFile(string Path) : FilePanelAction;
}

public static class FilePanelKeyBindings
{
    public static FilePanelAction? ActionFor(
        Key key,
        VisibleFileNode selected,
        bool searchActive)
    {
        if (searchActive || selected.Kind != FileNodeKind.File)
        {
            return null;
        }

        return Is(key, KeyCode.Enter) || Is(key, KeyCode.O)
            ? new FilePanelAction.OpenFile(selected.Files[0].Path)
            : null;
    }

    private static bool Is(Key key, KeyCode keyCode) => key.NoShift.KeyCode == keyCode;
}
