using TerminalDotnet.Search;

namespace TerminalDotnet.Changes;

public sealed class ChangesetSession(IChangesetBackend backend)
{
    private string target = "";
    private IReadOnlyList<ChangedFile> changedFiles = [];

    public ChangesetState State { get; private set; } = new([]) { Loading = true };

    public async Task LoadAsync(string target, CancellationToken cancellationToken = default)
    {
        this.target = target;
        try
        {
            State = await DiscoveredStateAsync(target, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            State = new ChangesetState([], 0, State.SearchQuery)
            {
                Notice = $"Could not read the changes: {exception.Message}"
            };
        }
    }

    /// <summary>A reload can arrive while the reader is part-way down the
    /// changeset, so the file they were on is found again rather than dropping
    /// them back to the top of a list that has grown underneath them.</summary>
    private async Task<ChangesetState> DiscoveredStateAsync(
        string target,
        CancellationToken cancellationToken)
    {
        var standingOn = Selected()?.DisplayPath;
        changedFiles = Snapshot.Of(await backend.DiscoverAsync(target, cancellationToken));
        var matching = Matching(State.SearchQuery);

        var selected = RowSelection.FoundAgain(matching, file => file.DisplayPath == standingOn, State.SelectedIndex);
        return new ChangesetState(matching, selected, State.SearchQuery)
        {
            Summary = SummaryFrom(changedFiles)
        };
    }

    public async Task DispatchAsync(
        ChangesetCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is ChangesetCommand.Search search)
        {
            State = State with
            {
                Files = Matching(search.Query),
                SelectedIndex = 0,
                SearchQuery = search.Query
            };
            return;
        }

        if (command is ChangesetCommand.ClearSearch)
        {
            State = State with { Files = changedFiles, SelectedIndex = 0, SearchQuery = "" };
            return;
        }

        if (command is ChangesetCommand.LoadSelectedDiff && Selected() is { } file)
        {
            State = State with { Diff = await DiffOfAsync(file, cancellationToken) };
            return;
        }

        if (command is ChangesetCommand.RestoreSelected &&
            Selected() is { Kind: ChangeKind.Deleted } deleted)
        {
            var restored = await backend.RestoreAsync(deleted, cancellationToken);
            await LoadAsync(target, cancellationToken);
            State = State with
            {
                Notice = restored ? "" : $"Could not restore {deleted.DisplayPath}"
            };
            return;
        }

        var rowCount = State.Files.Count;
        State = State with
        {
            SelectedIndex = command switch
            {
                ChangesetCommand.SelectIndex jump => RowSelection.At(jump.Index, rowCount),
                ChangesetCommand.MoveUp => RowSelection.Up(State.SelectedIndex),
                ChangesetCommand.MoveDown => RowSelection.Down(State.SelectedIndex, rowCount),
                _ => State.SelectedIndex
            }
        };
    }

    private async Task<DiffContext> DiffOfAsync(
        ChangedFile file,
        CancellationToken cancellationToken) =>
        new(file.DisplayPath, await backend.DiffAsync(file, cancellationToken));

    private ChangedFile? Selected() => State.SelectedIndex < State.Files.Count
        ? State.Files[State.SelectedIndex]
        : null;

    private IReadOnlyList<ChangedFile> Matching(string query) => query.Length == 0
        ? changedFiles
        : Snapshot.Of(changedFiles.Where(file => SearchMatch.Matches(file.DisplayPath, query)));

    private static ChangesetSummary SummaryFrom(IReadOnlyList<ChangedFile> files) => new(
        files.Count(file => file.Kind == ChangeKind.Modified),
        files.Count(file => file.Kind == ChangeKind.Added),
        files.Count(file => file.Kind == ChangeKind.Deleted));
}
