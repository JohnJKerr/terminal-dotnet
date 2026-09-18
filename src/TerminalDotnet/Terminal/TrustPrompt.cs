using Terminal.Gui.App;
using Terminal.Gui.Views;
using TerminalDotnet.Trust;

namespace TerminalDotnet.Terminal;

/// <summary>
/// Asks whether a folder may be built, before any panel has started. It runs
/// in a terminal session of its own because nothing else may begin until it
/// is answered. The box opens on its last choice, so a stray Enter quits, and
/// so does Esc. A console with no room for a box is asked in writing instead,
/// which is what a Windows console reporting no width gets.
/// </summary>
public static class TrustPrompt
{
    private const int TrustChoice = 0;

    public static bool Ask(TrustQuestion question)
    {
        if (!DialogSpace.FitsTheConsole())
        {
            return AskInWriting(question);
        }

        return TryAskInABox(question, out var trusted) ? trusted : AskInWriting(question);
    }

    /// <summary>
    /// Asks in a message box, unless the toolkit cannot size one for this
    /// console after all: it lays the box out against the width its driver
    /// reports, which is not the width <see cref="DialogSpace"/> reads.
    /// </summary>
    private static bool TryAskInABox(TrustQuestion question, out bool trusted)
    {
        try
        {
            using IApplication application = Application.Create();
            application.Init(TerminalDriverChoice.FromEnvironment());
            trusted = MessageBox.Query(
                application,
                question.Title,
                question.Message,
                question.TrustChoice,
                question.QuitChoice) == TrustChoice;
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            trusted = false;
            return false;
        }
    }

    private static bool AskInWriting(TrustQuestion question)
    {
        Console.Error.Write(question.AsText());
        return TrustQuestion.Trusts(Console.ReadLine());
    }
}
