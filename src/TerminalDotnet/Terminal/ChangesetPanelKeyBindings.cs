using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using TerminalDotnet.Changes;

namespace TerminalDotnet.Terminal;

public abstract record ChangesetAction
{
    public sealed record ShowDiff : ChangesetAction;
    public sealed record OpenFile(string Path) : ChangesetAction;
    public sealed record PreviewFile(string Path) : ChangesetAction;
    public sealed record RestoreFile(string Path) : ChangesetAction;
}

public static class ChangesetPanelKeyBindings
{
    public static ChangesetAction? ActionFor(Key key, ChangedFile selected, bool searchActive)
    {
        if (searchActive)
        {
            return null;
        }

        if (Is(key, KeyCode.Enter) || Is(key, KeyCode.D))
        {
            return new ChangesetAction.ShowDiff();
        }

        if (selected.Kind == ChangeKind.Deleted)
        {
            return Is(key, KeyCode.R) ? new ChangesetAction.RestoreFile(selected.Path) : null;
        }

        if (Is(key, KeyCode.E))
        {
            return new ChangesetAction.OpenFile(selected.Path);
        }

        return Is(key, KeyCode.P) ? new ChangesetAction.PreviewFile(selected.Path) : null;
    }

    private static bool Is(Key key, KeyCode keyCode) => key.NoShift.KeyCode == keyCode;
}
