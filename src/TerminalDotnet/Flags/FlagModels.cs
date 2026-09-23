namespace TerminalDotnet.Flags;

public enum FlagKind
{
    Todo, Fixme, Review, Question, Note, Warning, Warn, Hack, Xxx, Bug, Deprecated, Refactor, Optimize
}

public sealed record Flag(string Path, string DisplayPath, int Line, FlagKind Kind, string Comment)
{
    public string Heading => Kind == FlagKind.Warn ? "WARNING" : Kind.ToString().ToUpperInvariant();
}

public interface IFlagBackend
{
    Task<IReadOnlyList<Flag>> DiscoverAsync(string target, CancellationToken cancellationToken = default);
}
