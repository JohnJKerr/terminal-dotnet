using Terminal.Gui.App;
using Terminal.Gui.Views;
using TerminalDotnet.Trust;

namespace TerminalDotnet.Terminal;

/// <summary>
/// Asks whether a folder may be built, before any panel has started. It runs
/// in a terminal session of its own because nothing else may begin until it
/// is answered. The box opens on its last choice, so a stray Enter quits, and
/// so does Esc.
/// </summary>
public static class TrustPrompt
{
    private const int TrustChoice = 0;

    public static bool Ask(TrustQuestion question)
    {
        using IApplication application = Application.Create();
        application.Init(TerminalDriverChoice.FromEnvironment());
        return MessageBox.Query(
            application,
            question.Title,
            question.Message,
            question.TrustChoice,
            question.QuitChoice) == TrustChoice;
    }
}
