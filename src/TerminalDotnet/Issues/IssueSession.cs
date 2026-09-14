using TerminalDotnet.Comments;
using TerminalDotnet.Search;

namespace TerminalDotnet.Issues;

public sealed class IssueSession(IIssueBackend backend, ICommentClipboard clipboard)
{
    private IReadOnlyList<CompilationIssue> discovered = [];
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
            discovered = Snapshot.Of(await backend.DiscoverAsync(target, cancellationToken));
            State = State with { Issues = Matching(), Loading = false, Notice = "" };
            State = State with { SelectedIndex = RowFor(standingOn) };
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            State = State with { Issues = [], SelectedIndex = 0, Loading = false, Notice = exception.Message };
        }
    }

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
            IssueCommand.Search or IssueCommand.ClearSearch or IssueCommand.ToggleErrors or IssueCommand.ToggleWarnings => 0,
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
        _ => State.ActiveFilter
    };

    private IssueFilter? Toggled(IssueFilter filter) => State.ActiveFilter == filter ? null : filter;

    private IReadOnlyList<CompilationIssue> Matching() => [.. discovered.Where(issue =>
        MatchesFilter(issue) &&
        (State.SearchQuery.Length == 0 || SearchMatch.Matches(issue.Details, State.SearchQuery)))];

    private bool MatchesFilter(CompilationIssue issue) => State.ActiveFilter switch
    {
        IssueFilter.Errors => issue.Severity == IssueSeverity.Error,
        IssueFilter.Warnings => issue.Severity == IssueSeverity.Warning,
        _ => true
    };
}
