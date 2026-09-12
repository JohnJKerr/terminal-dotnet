using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using TerminalDotnet.Explorer;

namespace TerminalDotnet.Terminal;

public abstract record TestPanelAction
{
    public sealed record Dispatch(ExplorerCommand Command) : TestPanelAction;
    public sealed record CancelRun : TestPanelAction;
    public sealed record OpenSource : TestPanelAction;
    public sealed record PreviewSource : TestPanelAction;
    public sealed record ShowOutput : TestPanelAction;
}

public static class TestPanelKeyBindings
{
    public static TestPanelAction? ActionFor(
        Key key,
        string searchQuery,
        bool hasFocus)
    {
        if (hasFocus && searchQuery.Length > 0 && Is(key, KeyCode.N))
        {
            return Dispatched(new ExplorerCommand.NextSearchMatch());
        }

        if (hasFocus && searchQuery.Length > 0 && Is(key, KeyCode.B))
        {
            return Dispatched(new ExplorerCommand.PreviousSearchMatch());
        }

        if (hasFocus && FilterKeyBindings.FilterFor(key) is { } filter)
        {
            return Dispatched(new ExplorerCommand.ToggleFilter(filter));
        }

        if (hasFocus && Is(key, KeyCode.F))
        {
            return Dispatched(new ExplorerCommand.NextFailure());
        }

        return SourceActionFor(key) ?? (hasFocus ? RunActionFor(key) : null);
    }

    private static TestPanelAction? SourceActionFor(Key key)
    {
        if (Is(key, KeyCode.E))
        {
            return new TestPanelAction.OpenSource();
        }

        if (Is(key, KeyCode.P))
        {
            return new TestPanelAction.PreviewSource();
        }

        return Is(key, KeyCode.O) ? new TestPanelAction.ShowOutput() : null;
    }

    private static TestPanelAction? RunActionFor(Key key)
    {
        if (Is(key, KeyCode.CursorUp) || Is(key, KeyCode.K))
        {
            return Dispatched(new ExplorerCommand.MoveUp());
        }

        if (Is(key, KeyCode.CursorDown) || Is(key, KeyCode.J))
        {
            return Dispatched(new ExplorerCommand.MoveDown());
        }

        if (Is(key, KeyCode.Space))
        {
            return Dispatched(new ExplorerCommand.ToggleExpanded());
        }

        if (Is(key, KeyCode.Z))
        {
            return Dispatched(new ExplorerCommand.ToggleAllExpanded());
        }

        if (Is(key, KeyCode.Enter) || Is(key, KeyCode.R))
        {
            return Dispatched(new ExplorerCommand.RunSelected());
        }

        if (Is(key, KeyCode.L))
        {
            return Dispatched(new ExplorerCommand.RerunLast());
        }

        if (Is(key, KeyCode.U))
        {
            return Dispatched(new ExplorerCommand.RerunFailed());
        }

        return Is(key, KeyCode.C) ? new TestPanelAction.CancelRun() : null;
    }

    private static TestPanelAction Dispatched(ExplorerCommand command) =>
        new TestPanelAction.Dispatch(command);

    private static bool Is(Key key, KeyCode keyCode) =>
        !key.IsShift && key.NoShift.KeyCode == keyCode;
}
