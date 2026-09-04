using System.Globalization;
using TerminalDotnet.Explorer;
using TerminalDotnet.Testing;

namespace TerminalDotnet.Terminal;

public sealed record TestPanelSnapshot(
    string Target,
    string Breadcrumb,
    IReadOnlyList<VisibleTestNode> Tests,
    IReadOnlyList<string> TestRows,
    int SelectedIndex,
    string SearchQuery,
    int SearchHitCount,
    string StatusLine,
    string SelectedOutputTitle,
    string SelectedOutput,
    string EmptyMessage)
{
    public static TestPanelSnapshot From(ExplorerState state, string target) => new(
        Path.GetFileName(target),
        BreadcrumbFrom(state, target),
        state.VisibleNodes,
        TestRowsFrom(state),
        state.SelectedIndex,
        state.SearchQuery,
        state.VisibleNodes.Count(node => node.Kind == TestNodeKind.Test),
        StatusLineFrom(state),
        SelectedOutputTitleFrom(state),
        SelectedOutputFrom(state),
        PanelEmptyState.For("tests", state.VisibleNodes.Count, state.SearchQuery));

    private static string StatusLineFrom(ExplorerState state)
    {
        if (state.LastRun is null || state.Status == ExplorerStatus.Running)
        {
            return state.Message;
        }

        var summary = state.LastRun.Summary;
        return $"{summary.Failed} Failed, {summary.Passed} Passed, {summary.Skipped} Skipped";
    }

    private static string BreadcrumbFrom(ExplorerState state, string target)
    {
        if (state.VisibleNodes.Count == 0)
        {
            return Path.GetFileName(target);
        }

        var selected = state.VisibleNodes[state.SelectedIndex];
        var project = Path.GetFileNameWithoutExtension(selected.Tests[0].ProjectPath);
        return selected.Kind switch
        {
            TestNodeKind.Project => project,
            TestNodeKind.Class => $"{project} › {selected.Name}",
            _ => $"{project} › {selected.Tests[0].ClassName} › {selected.Name}"
        };
    }

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

    private static string SelectedOutputTitleFrom(ExplorerState state)
    {
        if (state.LastRun is null || state.VisibleNodes.Count == 0)
        {
            return "Test Output";
        }

        return $"Test Output — {state.VisibleNodes[state.SelectedIndex].Name}";
    }

    private static string SelectedOutputFrom(ExplorerState state)
    {
        if (state.LastRun is null || state.VisibleNodes.Count == 0)
        {
            return "No test output available.";
        }

        return state.LastRun.Output;
    }
}
