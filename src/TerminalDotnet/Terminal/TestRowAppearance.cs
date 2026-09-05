using Terminal.Gui.Drawing;
using TerminalDotnet.Explorer;

namespace TerminalDotnet.Terminal;

public static class TestRowAppearance
{
    public static Color ForegroundFor(TestNodeOutcome outcome, TestNodeUpdate update) => outcome switch
    {
        TestNodeOutcome.Failed => Color.BrightRed,
        TestNodeOutcome.Passed => Color.BrightGreen,
        TestNodeOutcome.Skipped => Color.BrightYellow,
        TestNodeOutcome.Running => Color.BrightCyan,
        _ => FileRowAppearance.ForegroundFor(ToneFor(update), Color.White)
    };

    private static FileRowTone ToneFor(TestNodeUpdate update) => update switch
    {
        TestNodeUpdate.Added => FileRowTone.New,
        TestNodeUpdate.Edited => FileRowTone.Modified,
        _ => FileRowTone.Neutral
    };
}
