using TerminalDotnet.Comments;
using TerminalDotnet.Search;

namespace TerminalDotnet.Issues;

public sealed class IssueSession(IIssueBackend backend, ICommentClipboard clipboard)
{
    private IReadOnlyList<CompilationIssue> discovered = [];
    public IssueState State { get; private set; } = new([]);

    public async Task LoadAsync(string target, CancellationToken cancellationToken = default)
    {
        try
        {
            discovered = Snapshot.Of(await backend.DiscoverAsync(target, cancellationToken));
            State = State with { Issues = Matching(), SelectedIndex = 0, Loading = false };
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            State = State with { Issues = [], Loading = false, Notice = exception.Message };
        }
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
