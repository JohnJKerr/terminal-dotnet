using Terminal.Gui.App;
using TerminalDotnet.Comments;

namespace TerminalDotnet.Terminal;

/// <summary>
/// The system clipboard, as the terminal framework reaches it. The terminal is
/// torn down and built again around an editor visit, so the clipboard is told
/// which application is running rather than holding one for the whole session.
/// It also depends on a helper the machine may not have, so a copy that does
/// not land is reported rather than thrown.
/// </summary>
internal sealed class TerminalClipboard : ICommentClipboard
{
    private IApplication? application;

    public void Attach(IApplication application) => this.application = application;

    public bool TryCopy(string text)
    {
        if (application is null)
        {
            return false;
        }

        try
        {
            return application.Clipboard.TrySetClipboardData(text);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return false;
        }
    }
}
