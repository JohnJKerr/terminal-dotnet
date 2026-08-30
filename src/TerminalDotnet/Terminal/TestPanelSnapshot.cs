using TerminalDotnet.Explorer;
using TerminalDotnet.Testing;

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
    string SearchQuery,
    IReadOnlyList<OutputLine> ResultLines,
    IReadOnlyList<OutputLine> OutputLines)
{
    public static TestPanelSnapshot From(ExplorerState state, string target) => new(
        Path.GetFileName(target),
        state.VisibleNodes,
        state.SelectedIndex,
        state.SearchQuery,
        ResultLinesFrom(state),
        state.Message
            .ReplaceLineEndings("\n")
            .Split('\n')
            .Select(OutputLineFrom)
            .ToArray());

    private static IReadOnlyList<OutputLine> ResultLinesFrom(ExplorerState state)
    {
        if (state.LastRun is null || state.VisibleNodes.Count == 0)
        {
            return [];
        }

        var selectedTests = state.VisibleNodes[state.SelectedIndex].Tests.ToHashSet();
        return state.LastRun.Results
            .Where(result => selectedTests.Contains(result.Test) && result.Outcome == TestOutcome.Failed)
            .SelectMany(FailureLines)
            .ToArray();
    }

    private static IEnumerable<OutputLine> FailureLines(TestResult failure)
    {
        yield return new OutputLine(
            $"✗ {failure.Test.DisplayName} — {failure.ErrorMessage ?? "Failed"}",
            OutputLineTone.Failure);
        if (failure.SourceFile is not null && failure.SourceLine is not null)
        {
            yield return new OutputLine(
                $"{failure.SourceFile}:{failure.SourceLine}",
                OutputLineTone.Neutral);
        }
    }

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
