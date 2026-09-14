using TerminalDotnet.Search;

namespace TerminalDotnet.Flags;

public sealed class FlagSession(IFlagBackend backend)
{
    private IReadOnlyList<Flag> discovered = [];

    public FlagState State { get; private set; } = new([]) { Loading = true };

    public async Task LoadAsync(string target, CancellationToken cancellationToken = default)
    {
        try
        {
            var standingOn = Selected();
            discovered = Snapshot.Of(await backend.DiscoverAsync(target, cancellationToken));
            Show(State.SearchQuery, State.ActiveFilter);
            State = State with { SelectedIndex = RowFor(standingOn) };
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            State = new FlagState([]);
        }
    }

    public Task DispatchAsync(FlagCommand command)
    {
        switch (command)
        {
            case FlagCommand.Search search: Show(search.Query, State.ActiveFilter); break;
            case FlagCommand.ClearSearch: Show("", State.ActiveFilter); break;
            case FlagCommand.ToggleFilter filter:
                Show(State.SearchQuery, State.ActiveFilter == filter.Category ? null : filter.Category);
                break;
            default: Move(command); break;
        }
        return Task.CompletedTask;
    }

    private void Show(string query, FlagCategory? filter) => State = new FlagState(
        Snapshot.Of(discovered
            .Where(flag => filter is null || flag.Category == filter)
            .Where(flag => SearchMatch.Matches(flag.DisplayPath, query) || SearchMatch.Matches(flag.Comment, query))
            .OrderBy(flag => flag.Kind)
            .ThenBy(flag => flag.DisplayPath, StringComparer.Ordinal)
            .ThenBy(flag => flag.Line)),
        SearchQuery: query,
        ActiveFilter: filter);

    private Flag? Selected() => State.SelectedIndex < State.Flags.Count
        ? State.Flags[State.SelectedIndex]
        : null;

    /// <summary>A reload can arrive while the reader is part-way down the list,
    /// so the flag they were on is found again wherever the edit moved it to.
    /// </summary>
    private int RowFor(Flag? standingOn)
    {
        var moved = standingOn is null
            ? -1
            : State.Flags.ToList().FindIndex(
                flag => flag.Path == standingOn.Path && flag.Line == standingOn.Line);
        return moved >= 0
            ? moved
            : Math.Clamp(State.SelectedIndex, 0, Math.Max(0, State.Flags.Count - 1));
    }

    private void Move(FlagCommand command)
    {
        var last = Math.Max(0, State.Flags.Count - 1);
        State = State with { SelectedIndex = command switch
        {
            FlagCommand.SelectIndex jump => Math.Clamp(jump.Index, 0, last),
            FlagCommand.MoveUp => Math.Max(0, State.SelectedIndex - 1),
            FlagCommand.MoveDown => Math.Min(last, State.SelectedIndex + 1),
            _ => State.SelectedIndex
        }};
    }
}
