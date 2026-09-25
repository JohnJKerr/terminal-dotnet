using TerminalDotnet.Comments;
using TerminalDotnet.Flags;
using TerminalDotnet.Search;

namespace TerminalDotnet.Issues;

/// <summary>
/// What the build reported and what the source flags for attention, read as
/// one list. The two are loaded apart: a flag is found by reading the tree, so
/// it answers an edit at once, while a build issue waits for the next build.
/// </summary>
public sealed class IssueSession(
    IIssueBackend backend,
    ICommentClipboard clipboard,
    IFlagBackend flagBackend)
{
    private IReadOnlyList<CompilationIssue> built = [];
    private IReadOnlyList<CompilationIssue> flagged = [];
    public IssueState State { get; private set; } = new([]);

    /// <summary>Stands the panel back up as building. The issues the last build
    /// found stay in front of the reader while it runs, because a build takes
    /// long enough that emptying the panel would leave them with nothing to read.
    /// </summary>
    public void Rebuilding() => State = State with { Loading = true, Notice = "" };

    public async Task LoadAsync(string target, CancellationToken cancellationToken = default)
    {
        try
        {
            var standingOn = Selected();
            built = Snapshot.Of(await backend.DiscoverAsync(target, cancellationToken));
            State = State with { Issues = Matching(), Loading = false, Notice = "" };
            State = State with { SelectedIndex = RowFor(standingOn) };
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            built = [];
            State = State with { Issues = Matching(), SelectedIndex = 0, Loading = false, Notice = exception.Message };
        }
    }

    /// <summary>A tree that cannot be read has no flags to offer, which is no
    /// reason to take the build's issues away with them.</summary>
    public async Task LoadFlagsAsync(string target, CancellationToken cancellationToken = default)
    {
        var standingOn = Selected();
        try
        {
            flagged = Snapshot.Of(FlagIssues(await flagBackend.DiscoverAsync(target, cancellationToken)));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            flagged = [];
        }

        State = State with { Issues = Matching(), FlagsLoading = false };
        State = State with { SelectedIndex = RowFor(standingOn) };
    }

    private static IEnumerable<CompilationIssue> FlagIssues(IReadOnlyList<Flag> flags) => flags
        .OrderBy(flag => flag.Kind)
        .ThenBy(flag => flag.DisplayPath, StringComparer.Ordinal)
        .ThenBy(flag => flag.Line)
        .Select(FlagIssue);

    private static CompilationIssue FlagIssue(Flag flag) => new(
        flag.Path,
        flag.DisplayPath,
        flag.Line,
        0,
        flag.Heading,
        flag.Comment,
        IssueSeverity.Flag);

    private CompilationIssue? Selected() => State.SelectedIndex < State.Issues.Count
        ? State.Issues[State.SelectedIndex]
        : null;

    /// <summary>A rebuild can land while the reader is part-way down the list,
    /// so the issue they were on is found again wherever the build moved it to.
    /// </summary>
    private int RowFor(CompilationIssue? standingOn)
    {
        var moved = standingOn is null
            ? -1
            : State.Issues.ToList().FindIndex(issue => issue.Details == standingOn.Details);
        return moved >= 0
            ? moved
            : Math.Clamp(State.SelectedIndex, 0, Math.Max(0, State.Issues.Count - 1));
    }

    public async Task DispatchAsync(IssueCommand command, CancellationToken cancellationToken = default)
    {
        var query = command switch
        {
            IssueCommand.Search search => search.Query,
            IssueCommand.ClearSearch => "",
            _ => State.SearchQuery
        };
        State = State with
        {
            SearchQuery = query,
            ActiveFilter = FilterAfter(command)
        };
        var visible = Matching();
        var last = Math.Max(0, visible.Count - 1);
        var index = command switch
        {
            IssueCommand.Search or IssueCommand.ClearSearch or IssueCommand.ToggleErrors or IssueCommand.ToggleWarnings or IssueCommand.ToggleFlags => 0,
            IssueCommand.SelectIndex select => Math.Clamp(select.Index, 0, last),
            IssueCommand.MoveUp => Math.Max(0, State.SelectedIndex - 1),
            IssueCommand.MoveDown => Math.Min(last, State.SelectedIndex + 1),
            _ => Math.Min(State.SelectedIndex, last)
        };
        State = State with { Issues = visible, SelectedIndex = index, Notice = "" };
        if (command is IssueCommand.CopySelected && visible.Count > 0)
        {
            var copied = await clipboard.TryCopyAsync(visible[index].Details, cancellationToken);
            State = State with { Notice = copied ? "Copied issue details" : "Could not copy issue details" };
        }
    }

    private IssueFilter? FilterAfter(IssueCommand command) => command switch
    {
        IssueCommand.ToggleErrors => Toggled(IssueFilter.Errors),
        IssueCommand.ToggleWarnings => Toggled(IssueFilter.Warnings),
        IssueCommand.ToggleFlags => Toggled(IssueFilter.Flags),
        _ => State.ActiveFilter
    };

    private IssueFilter? Toggled(IssueFilter filter) => State.ActiveFilter == filter ? null : filter;

    private IReadOnlyList<CompilationIssue> Matching() => [.. built.Concat(flagged).Where(issue =>
        MatchesFilter(issue) &&
        (State.SearchQuery.Length == 0 || SearchMatch.Matches(issue.Details, State.SearchQuery)))];

    private bool MatchesFilter(CompilationIssue issue) => State.ActiveFilter switch
    {
        IssueFilter.Errors => issue.Severity == IssueSeverity.Error,
        IssueFilter.Warnings => issue.Severity == IssueSeverity.Warning,
        IssueFilter.Flags => issue.Severity == IssueSeverity.Flag,
        _ => true
    };
}
