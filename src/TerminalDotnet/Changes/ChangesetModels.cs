namespace TerminalDotnet.Changes;

public enum ChangeKind
{
    Added,
    Modified,
    Deleted
}

public sealed record ChangedFile(string Path, string DisplayPath, ChangeKind Kind);

public sealed record ChangesetSummary(int Changed, int Added, int Deleted)
{
    public static readonly ChangesetSummary Empty = new(0, 0, 0);
}

public sealed record DiffContext(string DisplayPath, string Diff);

public sealed record ChangesetState(
    IReadOnlyList<ChangedFile> Files,
    int SelectedIndex = 0,
    string SearchQuery = "")
{
    public ChangesetSummary Summary { get; init; } = ChangesetSummary.Empty;

    public DiffContext? Diff { get; init; }
}

public abstract record ChangesetCommand
{
    public sealed record Search(string Query) : ChangesetCommand;
    public sealed record ClearSearch : ChangesetCommand;
    public sealed record MoveUp : ChangesetCommand;
    public sealed record MoveDown : ChangesetCommand;
    public sealed record LoadSelectedDiff : ChangesetCommand;
    public sealed record RestoreSelected : ChangesetCommand;
}

public interface IChangesetBackend
{
    Task<IReadOnlyList<ChangedFile>> DiscoverAsync(
        string target,
        CancellationToken cancellationToken = default);

    Task<string> DiffAsync(ChangedFile file, CancellationToken cancellationToken = default);

    Task RestoreAsync(ChangedFile file, CancellationToken cancellationToken = default);
}
