using TerminalDotnet.Explorer;

namespace TerminalDotnet.Terminal;

/// <summary>Shows the active run over whichever panel the reader is viewing.</summary>
public static class TestRunToast
{
    public static Toast Running(ExplorerState tests, TimeSpan elapsed) =>
        new(ActivityMarker.Marking(tests.Message, elapsed), ToastTone.Working);

    public static Toast Finished(ExplorerState tests) => tests switch
    {
        { Message: "Run cancelled" } => new("Test run cancelled", ToastTone.Waiting),
        { Status: ExplorerStatus.Ready } => new("Tests passed", ToastTone.Succeeded),
        _ => new("Test run failed", ToastTone.Failed)
    };
}
