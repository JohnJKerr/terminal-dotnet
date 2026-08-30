using System.Globalization;
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
    IReadOnlyList<string> TestRows,
    int SelectedIndex,
    string SearchQuery,
    int SearchHitCount,
    IReadOnlyList<OutputLine> ResultLines,
    IReadOnlyList<OutputLine> OutputLines)
{
    public static TestPanelSnapshot From(ExplorerState state, string target) => new(
        Path.GetFileName(target),
        state.VisibleNodes,
        TestRowsFrom(state),
        state.SelectedIndex,
        state.SearchQuery,
        state.VisibleNodes.Count(node => node.Kind == TestNodeKind.Test),
        ResultLinesFrom(state),
        state.Message
            .ReplaceLineEndings("\n")
            .Split('\n')
            .Select(OutputLineFrom)
            .ToArray());

    private static IReadOnlyList<string> TestRowsFrom(ExplorerState state)
    {
        var results = state.LastRun?.Results
            .GroupBy(result => result.Test)
            .ToDictionary(group => group.Key, group => group.First()) ?? [];
        return state.VisibleNodes
            .Select(node => TestRow(node, results))
            .ToArray();
    }

    private static string TestRow(
        VisibleTestNode node,
        IReadOnlyDictionary<TestCase, TestResult> results)
    {
        var marker = node.Outcome switch
        {
            TestNodeOutcome.Running => "◌",
            TestNodeOutcome.Passed => "✓",
            TestNodeOutcome.Skipped => "○",
            TestNodeOutcome.Failed => "✗",
            _ when node.Kind == TestNodeKind.Test => "•",
            _ when !node.IsExpanded => "▶",
            _ => "▼"
        };
        var metadata = node.Kind switch
        {
            TestNodeKind.Test when results.TryGetValue(node.Tests[0], out var result) =>
                $" {result.Duration.TotalMilliseconds.ToString("0.#", CultureInfo.InvariantCulture)}ms",
            TestNodeKind.Test => "",
            _ => $" {node.Tests.Count}"
        };
        return $"{new string(' ', node.Depth * 2)}{marker} {node.Name}{metadata}";
    }

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
