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
    public sealed record AwaitFailureNavigation : TestPanelAction;
}

public static class TestPanelKeyBindings
{
    public static TestPanelAction? ActionFor(
        Key key,
        string searchQuery,
        bool hasFocus,
        bool awaitingFailureNavigation)
    {
        if (hasFocus && searchQuery.Length > 0 && Is(key, KeyCode.N))
        {
            return Dispatched(key.IsShift
                ? new ExplorerCommand.PreviousSearchMatch()
                : new ExplorerCommand.NextSearchMatch());
        }

        if (hasFocus && FilterKeyBindings.FilterFor(key) is { } filter)
        {
            return Dispatched(new ExplorerCommand.ToggleFilter(filter));
        }

        if (hasFocus && awaitingFailureNavigation && Is(key, KeyCode.F))
        {
            return Dispatched(new ExplorerCommand.NextFailure());
        }

        if (hasFocus && Is(key, (KeyCode)']'))
        {
            return new TestPanelAction.AwaitFailureNavigation();
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

        if (Is(key, KeyCode.Enter) || Is(key, KeyCode.R) && !key.IsShift)
        {
            return Dispatched(new ExplorerCommand.RunSelected());
        }

        if (Is(key, KeyCode.R) && key.IsShift)
        {
            return Dispatched(new ExplorerCommand.RerunLast());
        }

        if (Is(key, KeyCode.F) && key.IsShift)
        {
            return Dispatched(new ExplorerCommand.RerunFailed());
        }

        return Is(key, KeyCode.C) ? new TestPanelAction.CancelRun() : null;
    }

    private static TestPanelAction Dispatched(ExplorerCommand command) =>
        new TestPanelAction.Dispatch(command);

    private static bool Is(Key key, KeyCode keyCode) => key.NoShift.KeyCode == keyCode;
}
