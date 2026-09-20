using Terminal.Gui.Drivers;
using Terminal.Gui.Input;

namespace TerminalDotnet.Terminal;

public abstract record DiffAction
{
    /// <summary>Move to another of the changeset's files. One step forward or
    /// back through the rows the panel is showing.</summary>
    public sealed record StepFile(int Step) : DiffAction;

    public sealed record Scroll(int Rows) : DiffAction;

    public sealed record Comment : DiffAction;
}

/// <summary>
/// What a key does while a diff is open. The diff keeps its own bindings
/// because the panel beneath it is not listening while it is up. Only the keys
/// the view showing the diff has nothing of its own for are named here; the
/// arrows, Page keys and Esc are already its own.
/// </summary>
public static class DiffKeyBindings
{
    public static DiffAction? ActionFor(Key key) => key.NoShift.KeyCode switch
    {
        KeyCode.N => new DiffAction.StepFile(key.IsShift ? -1 : 1),
        KeyCode.J => new DiffAction.Scroll(1),
        KeyCode.K => new DiffAction.Scroll(-1),
        KeyCode.C => new DiffAction.Comment(),
        _ => null
    };
}
