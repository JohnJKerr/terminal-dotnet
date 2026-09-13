using Terminal.Gui.Drivers;
using Terminal.Gui.Input;

namespace TerminalDotnet.Terminal;

public abstract record PreviewAction
{
    public sealed record Scroll(int Rows) : PreviewAction;
    public sealed record ScrollToStart : PreviewAction;
    public sealed record ScrollToEnd : PreviewAction;
    public sealed record Edit : PreviewAction;
    public sealed record Comment : PreviewAction;
}

/// <summary>
/// What a key does while a file is open in the preview. The preview keeps its
/// own bindings because the panel beneath it is not listening while it is up.
/// </summary>
public static class PreviewKeyBindings
{
    public static PreviewAction? ActionFor(Key key, int viewportHeight) => key.NoShift.KeyCode switch
    {
        KeyCode.CursorDown or KeyCode.J => new PreviewAction.Scroll(1),
        KeyCode.CursorUp or KeyCode.K => new PreviewAction.Scroll(-1),
        KeyCode.PageDown => new PreviewAction.Scroll(PageRows(viewportHeight)),
        KeyCode.PageUp => new PreviewAction.Scroll(-PageRows(viewportHeight)),
        KeyCode.Home => new PreviewAction.ScrollToStart(),
        KeyCode.End => new PreviewAction.ScrollToEnd(),
        KeyCode.E => new PreviewAction.Edit(),
        KeyCode.C => new PreviewAction.Comment(),
        _ => null
    };

    /// <summary>A page keeps one row of the last screen, so the reader has a
    /// line of context to carry across the jump.</summary>
    private static int PageRows(int viewportHeight) => Math.Max(1, viewportHeight - 1);
}
