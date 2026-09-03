using Terminal.Gui.Drawing;
using TerminalDotnet.Explorer;

namespace TerminalDotnet.Terminal;

public static class TestStatusAppearance
{
    public static Color ForegroundFor(ExplorerState state) => state.Status switch
    {
        ExplorerStatus.Failed => Color.BrightRed,
        ExplorerStatus.Running => Color.BrightCyan,
        _ when state.LastRun?.Passed == true => Color.BrightGreen,
        _ => Color.White
    };
}
