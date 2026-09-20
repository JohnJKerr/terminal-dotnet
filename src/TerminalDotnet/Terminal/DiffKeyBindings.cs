using Terminal.Gui.Drivers;
using Terminal.Gui.Input;

namespace TerminalDotnet.Terminal;

public abstract record DiffAction
{
    /// <summary>Move to another of the changeset's files. One step forward or
    /// back through the rows the panel is showing.</summary>
    public sealed record StepFile(int Step) : DiffAction;

    public sealed record Scroll(int Rows) : DiffAction;

    public sealed record ScrollToStart : DiffAction;

    public sealed record ScrollToEnd : DiffAction;

    public sealed record Comment : DiffAction;
}

/// <summary>
/// What a key does while a diff is open. The diff keeps its own bindings
/// because the panel beneath it is not listening while it is up.
/// </summary>
public static class DiffKeyBindings
{
    public static DiffAction? ActionFor(Key key, int viewportHeight) => key.NoShift.KeyCode switch
    {
        KeyCode.N => new DiffAction.StepFile(key.IsShift ? -1 : 1),
        KeyCode.CursorDown or KeyCode.J => new DiffAction.Scroll(1),
        KeyCode.CursorUp or KeyCode.K => new DiffAction.Scroll(-1),
        KeyCode.PageDown => new DiffAction.Scroll(PageRows(viewportHeight)),
        KeyCode.PageUp => new DiffAction.Scroll(-PageRows(viewportHeight)),
        KeyCode.Home => new DiffAction.ScrollToStart(),
        KeyCode.End => new DiffAction.ScrollToEnd(),
        KeyCode.C => new DiffAction.Comment(),
        _ => null
    };

    private static int PageRows(int viewportHeight) => Math.Max(1, viewportHeight - 1);
}
