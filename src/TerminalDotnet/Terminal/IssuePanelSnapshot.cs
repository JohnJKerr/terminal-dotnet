using TerminalDotnet.Issues;

namespace TerminalDotnet.Terminal;

public sealed record IssuePanelRow(string Text, FileRowTone Tone);

public sealed record IssuePanelLayout(IReadOnlyList<IssuePanelRow> Rows, int SelectedRowIndex)
{
    public static IssuePanelLayout From(IssuePanelSnapshot snapshot, int width)
    {
        var displayed = snapshot.Issues.Select(issue => LinesFor(issue, Math.Max(1, width))).ToArray();
        var rows = displayed.SelectMany((lines, index) => index == displayed.Length - 1
            ? lines
            : [.. lines, new IssuePanelRow("", FileRowTone.Neutral)]).ToArray();
        var selected = displayed.Take(snapshot.SelectedIndex).Sum(lines => lines.Count + 1);
        return new IssuePanelLayout(rows, selected);
    }

    private static IReadOnlyList<IssuePanelRow> LinesFor(CompilationIssue issue, int width)
    {
        var tone = issue.Severity == IssueSeverity.Error ? FileRowTone.Deleted : FileRowTone.Warning;
        return [.. Wrapped(issue.Details, width).Select(line => new IssuePanelRow(line, tone))];
    }

    private static IEnumerable<string> Wrapped(string text, int width)
    {
        var remaining = text.Trim();
        while (remaining.Length > width)
        {
            var breakAt = remaining.LastIndexOf(' ', width);
            breakAt = breakAt <= 0 ? width : breakAt;
            yield return remaining[..breakAt];
            remaining = remaining[breakAt..].TrimStart();
        }

        yield return remaining;
    }
}

public sealed record IssuePanelSnapshot(
    IReadOnlyList<CompilationIssue> Issues, int SelectedIndex, string SearchQuery,
    IReadOnlyList<FileStatusSegment> StatusSegments, IReadOnlyList<FilterChip> Filters, string EmptyMessage)
{
    public string SelectedDetails => SelectedIndex < Issues.Count ? Issues[SelectedIndex].Details : "";

    public IReadOnlyList<IssuePanelRow> Rows => [.. Issues.Select(issue => new IssuePanelRow(
        issue.Details, issue.Severity == IssueSeverity.Error ? FileRowTone.Deleted : FileRowTone.Warning))];

    public static IssuePanelSnapshot From(IssueState state) => new(
        state.Issues, state.SelectedIndex, state.SearchQuery,
        [new($"{state.Issues.Count(x => x.Severity == IssueSeverity.Error)} Errors", FileRowTone.Deleted),
         new($"{state.Issues.Count(x => x.Severity == IssueSeverity.Warning)} Warnings", FileRowTone.Warning),
         .. state.Loading ? new[] { new FileStatusSegment("Building", FileRowTone.Neutral) } : [],
         .. state.Notice.Length == 0 ? [] : new[] { new FileStatusSegment(state.Notice, FileRowTone.Neutral) }],
        [new("1. Errors", state.ActiveFilter == IssueFilter.Errors),
         new("2. Warnings", state.ActiveFilter == IssueFilter.Warnings)],
        state.Loading ? "" : PanelEmptyState.For("issues", state.Issues.Count, state.SearchQuery));
}
