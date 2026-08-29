using TerminalDotnet.Explorer;

namespace TerminalDotnet.Terminal;

public sealed record TestPanelSnapshot(
    string Target,
    IReadOnlyList<VisibleTestNode> Tests,
    int SelectedIndex,
    IReadOnlyList<string> OutputLines)
{
    public static TestPanelSnapshot From(ExplorerState state, string target) => new(
        Path.GetFileName(target),
        state.VisibleNodes,
        state.SelectedIndex,
        state.Message.ReplaceLineEndings("\n").Split('\n'));
}
