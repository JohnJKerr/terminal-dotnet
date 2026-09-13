using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using TerminalDotnet.Comments;

namespace TerminalDotnet.Terminal;

public abstract record CommentAction
{
    public sealed record ReadComment : CommentAction;
    public sealed record RewriteComment : CommentAction;
    public sealed record DeleteComment : CommentAction;
    public sealed record CopyComments : CommentAction;
}

public static class CommentPanelKeyBindings
{
    public static CommentAction? ActionFor(Key key, FileComment? selected, bool searchActive)
    {
        if (searchActive || selected is null)
        {
            return null;
        }

        if (Is(key, KeyCode.Enter) || Is(key, KeyCode.V))
        {
            return new CommentAction.ReadComment();
        }

        if (Is(key, KeyCode.E))
        {
            return new CommentAction.RewriteComment();
        }

        if (Is(key, KeyCode.D))
        {
            return new CommentAction.DeleteComment();
        }

        return Is(key, KeyCode.Y) ? new CommentAction.CopyComments() : null;
    }

    private static bool Is(Key key, KeyCode keyCode) =>
        !key.IsShift && key.NoShift.KeyCode == keyCode;
}
