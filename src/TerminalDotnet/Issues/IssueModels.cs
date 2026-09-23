namespace TerminalDotnet.Issues;

public enum IssueSeverity { Error, Warning, Flag }

public enum IssueFilter { Errors, Warnings, Flags }

public sealed record CompilationIssue(
    string Path,
    string DisplayPath,
    int Line,
    int Column,
    string Code,
    string Message,
    IssueSeverity Severity)
{
    public string Details => Severity == IssueSeverity.Flag
        ? $"{DisplayPath}:{Line}: {Code} {Message}".TrimEnd()
        : $"{DisplayPath}({Line},{Column}): {Severity.ToString().ToLowerInvariant()} {Code}: {Message}";
}

public sealed record IssueState(
    IReadOnlyList<CompilationIssue> Issues,
    int SelectedIndex = 0,
    string SearchQuery = "")
{
    public IssueFilter? ActiveFilter { get; init; }
    public bool Loading { get; init; } = true;
    public string Notice { get; init; } = "";
}

public abstract record IssueCommand
{
    public sealed record Search(string Query) : IssueCommand;
    public sealed record ClearSearch : IssueCommand;
    public sealed record SelectIndex(int Index) : IssueCommand;
    public sealed record MoveUp : IssueCommand;
    public sealed record MoveDown : IssueCommand;
    public sealed record ToggleErrors : IssueCommand;
    public sealed record ToggleWarnings : IssueCommand;
    public sealed record ToggleFlags : IssueCommand;
    public sealed record CopySelected : IssueCommand;
}

public interface IIssueBackend
{
    Task<IReadOnlyList<CompilationIssue>> DiscoverAsync(string target, CancellationToken cancellationToken = default);
}
