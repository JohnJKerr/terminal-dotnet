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
        _ => RowAppearance.ForegroundFor(ToneFor(update), Color.White)
    };

    private static RowTone ToneFor(TestNodeUpdate update) => update switch
    {
        TestNodeUpdate.Added => RowTone.New,
        TestNodeUpdate.Edited => RowTone.Modified,
        _ => RowTone.Neutral
    };
}
