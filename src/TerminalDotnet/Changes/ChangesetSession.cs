using TerminalDotnet.Search;

namespace TerminalDotnet.Changes;

public sealed class ChangesetSession(IChangesetBackend backend)
{
    private string target = "";
    private IReadOnlyList<ChangedFile> changedFiles = [];

    public ChangesetState State { get; private set; } = new([]);

    public async Task LoadAsync(string target, CancellationToken cancellationToken = default)
    {
        this.target = target;
        changedFiles = await backend.DiscoverAsync(target, cancellationToken);
        State = new ChangesetState(Matching(State.SearchQuery), 0, State.SearchQuery)
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
            var diff = await backend.DiffAsync(file, cancellationToken);
            State = State with { Diff = new DiffContext(file.DisplayPath, diff) };
            return;
        }

        if (command is ChangesetCommand.RestoreSelected &&
            Selected() is { Kind: ChangeKind.Deleted } deleted)
        {
            await backend.RestoreAsync(deleted, cancellationToken);
            await LoadAsync(target, cancellationToken);
            return;
        }

        var lastIndex = Math.Max(0, State.Files.Count - 1);
        State = State with
        {
            SelectedIndex = command switch
            {
                ChangesetCommand.MoveUp => Math.Max(0, State.SelectedIndex - 1),
                ChangesetCommand.MoveDown => Math.Min(lastIndex, State.SelectedIndex + 1),
                _ => State.SelectedIndex
            }
        };
    }

    private ChangedFile? Selected() => State.SelectedIndex < State.Files.Count
        ? State.Files[State.SelectedIndex]
        : null;

    private IReadOnlyList<ChangedFile> Matching(string query) => query.Length == 0
        ? changedFiles
        : changedFiles.Where(file => SearchMatch.Matches(file.DisplayPath, query)).ToArray();

    private static ChangesetSummary SummaryFrom(IReadOnlyList<ChangedFile> files) => new(
        files.Count(file => file.Kind == ChangeKind.Modified),
        files.Count(file => file.Kind == ChangeKind.Added),
        files.Count(file => file.Kind == ChangeKind.Deleted));
}
