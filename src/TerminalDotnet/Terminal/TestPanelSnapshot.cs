using TerminalDotnet.Explorer;
using TerminalDotnet.Testing;

namespace TerminalDotnet.Terminal;

public sealed record TestPanelResult(
    string Title,
    string Summary,
    string Details);

public sealed record TestPanelSnapshot(
    string Target,
    IReadOnlyList<VisibleTestNode> Tests,
    int SelectedIndex,
    TestPanelResult Result,
    string Output)
{
    public static TestPanelSnapshot From(ExplorerState state, string target)
    {
        var selectedTests = state.VisibleNodes.Count == 0
            ? []
            : state.VisibleNodes[state.SelectedIndex].Tests;
        var selectedResult = state.LastRun?.Results
            .FirstOrDefault(result => selectedTests.Contains(result.Test));

        return new TestPanelSnapshot(
            Path.GetFileName(target),
            state.VisibleNodes,
            state.SelectedIndex,
            ResultFrom(selectedResult, state),
            state.Message);
    }

    private static TestPanelResult ResultFrom(TestResult? result, ExplorerState state)
    {
        if (result is null)
        {
            return new TestPanelResult("Result", state.Status.ToString(), "No result for the selected item.");
        }

        var summary = $"{result.Outcome} · {result.Duration.TotalMilliseconds:0} ms";
        var details = string.Join(
            Environment.NewLine + Environment.NewLine,
            new[] { result.ErrorMessage, SourceLocation(result), result.StackTrace }
                .Where(value => !string.IsNullOrWhiteSpace(value)));
        return new TestPanelResult(result.Test.DisplayName, summary, details);
    }

    private static string? SourceLocation(TestResult result) => result.SourceFile is null
        ? null
        : $"{result.SourceFile}:{result.SourceLine}";
}
