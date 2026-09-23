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
        var tone = IssuePanelSnapshot.ToneFor(issue);
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

    public IReadOnlyList<IssuePanelRow> Rows =>
        [.. Issues.Select(issue => new IssuePanelRow(issue.Details, ToneFor(issue)))];

    public static FileRowTone ToneFor(CompilationIssue issue) => issue.Severity switch
    {
        IssueSeverity.Error => FileRowTone.Deleted,
        IssueSeverity.Warning => FileRowTone.Warning,
        _ => FileRowTone.Neutral
    };

    public static IssuePanelSnapshot From(IssueState state) => new(
        state.Issues, state.SelectedIndex, state.SearchQuery,
        [new(CountedNoun.Of(state.Issues.Count(x => x.Severity == IssueSeverity.Error), "Error"), FileRowTone.Deleted),
         new(CountedNoun.Of(state.Issues.Count(x => x.Severity == IssueSeverity.Warning), "Warning"), FileRowTone.Warning),
         new(CountedNoun.Of(state.Issues.Count(x => x.Severity == IssueSeverity.Flag), "Flag"), FileRowTone.Neutral),
         .. state.Notice.Length == 0 ? [] : new[] { new FileStatusSegment(state.Notice, FileRowTone.Neutral) }],
        [new("X Errors", state.ActiveFilter == IssueFilter.Errors),
         new("W Warnings", state.ActiveFilter == IssueFilter.Warnings),
         new("F Flags", state.ActiveFilter == IssueFilter.Flags)],
        state.Loading ? "" : PanelEmptyState.For("issues", state.Issues.Count, state.SearchQuery));
}
