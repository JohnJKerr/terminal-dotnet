using TerminalDotnet.Explorer;

namespace TerminalDotnet.Terminal;

public enum OutputLineTone
{
    Neutral,
    Failure,
    Success,
    Skipped,
    Status
}

public sealed record OutputLine(string Text, OutputLineTone Tone);

public sealed record TestPanelSnapshot(
    string Target,
    IReadOnlyList<VisibleTestNode> Tests,
    int SelectedIndex,
    IReadOnlyList<OutputLine> OutputLines)
{
    public static TestPanelSnapshot From(ExplorerState state, string target) => new(
        Path.GetFileName(target),
        state.VisibleNodes,
        state.SelectedIndex,
        state.Message
            .ReplaceLineEndings("\n")
            .Split('\n')
            .Select(OutputLineFrom)
            .ToArray());

    private static OutputLine OutputLineFrom(string text) => new(text, ToneFrom(text.TrimStart()));

    private static OutputLineTone ToneFrom(string text)
    {
        if (text.StartsWith("Failed", StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith("Error Message:", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("[FAIL]", StringComparison.OrdinalIgnoreCase))
        {
            return OutputLineTone.Failure;
        }

        if (text.StartsWith("Passed", StringComparison.OrdinalIgnoreCase))
        {
            return OutputLineTone.Success;
        }

        if (text.StartsWith("Skipped", StringComparison.OrdinalIgnoreCase))
        {
            return OutputLineTone.Skipped;
        }

        return text.StartsWith("Running", StringComparison.OrdinalIgnoreCase)
            ? OutputLineTone.Status
            : OutputLineTone.Neutral;
    }
}
