using TerminalDotnet.Issues;

namespace TerminalDotnet.Terminal;

public sealed record IssuePanelRow(string Text, FileRowTone Tone);

public sealed record IssuePanelLayout(
    IReadOnlyList<IssuePanelRow> Rows,
    int SelectedRowIndex,
    IReadOnlyList<int> FirstRows)
{
    /// <summary>The issue a row belongs to. The gap beneath an issue counts
    /// as part of it, so a click there lands on the issue above.</summary>
    public int IssueAt(int row) => Math.Max(0, FirstRows.Count(first => first <= row) - 1);

    public static IssuePanelLayout From(IssuePanelSnapshot snapshot, int width)
    {
        var displayed = snapshot.Issues.Select(issue => LinesFor(issue, Math.Max(1, width))).ToArray();
        var rows = displayed.SelectMany((lines, index) => index == displayed.Length - 1
            ? lines
            : [.. lines, new IssuePanelRow("", FileRowTone.Neutral)]).ToArray();
        var firstRows = FirstRowsOf(displayed);
        var selected = snapshot.SelectedIndex < firstRows.Length ? firstRows[snapshot.SelectedIndex] : 0;
        return new IssuePanelLayout(rows, selected, firstRows);
    }

    /// <summary>Where each issue starts, counting the gap left beneath every
    /// issue before it.</summary>
    private static int[] FirstRowsOf(IReadOnlyList<IReadOnlyList<IssuePanelRow>> displayed)
    {
        var firstRows = new int[displayed.Count];
        for (var index = 1; index < displayed.Count; index++)
        {
            firstRows[index] = firstRows[index - 1] + displayed[index - 1].Count + 1;
        }

        return firstRows;
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
