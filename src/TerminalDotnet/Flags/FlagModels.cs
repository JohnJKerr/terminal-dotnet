namespace TerminalDotnet.Flags;

public enum FlagCategory { Tasks, Review, Warning, Improve }

public enum FlagKind
{
    Todo, Fixme, Review, Question, Note, Warning, Warn, Hack, Xxx, Bug, Deprecated, Refactor, Optimize
}

public sealed record Flag(string Path, string DisplayPath, int Line, FlagKind Kind, string Comment)
{
    public string Heading => Kind == FlagKind.Warn ? "WARNING" : Kind.ToString().ToUpperInvariant();

    public FlagCategory Category => Kind switch
    {
        FlagKind.Todo or FlagKind.Fixme => FlagCategory.Tasks,
        FlagKind.Review or FlagKind.Question or FlagKind.Note => FlagCategory.Review,
        FlagKind.Refactor or FlagKind.Optimize => FlagCategory.Improve,
        _ => FlagCategory.Warning
    };
}

public sealed record FlagState(
    IReadOnlyList<Flag> Flags,
    int SelectedIndex = 0,
    string SearchQuery = "",
    FlagCategory? ActiveFilter = null)
{
    public bool Loading { get; init; }
}

public abstract record FlagCommand
{
    public sealed record Search(string Query) : FlagCommand;
    public sealed record ClearSearch : FlagCommand;
    public sealed record ToggleFilter(FlagCategory Category) : FlagCommand;
    public sealed record SelectIndex(int Index) : FlagCommand;
    public sealed record MoveUp : FlagCommand;
    public sealed record MoveDown : FlagCommand;
}

public interface IFlagBackend
{
    Task<IReadOnlyList<Flag>> DiscoverAsync(string target, CancellationToken cancellationToken = default);
}
