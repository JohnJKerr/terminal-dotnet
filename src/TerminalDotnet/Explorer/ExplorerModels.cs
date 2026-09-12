using TerminalDotnet.Filters;
using TerminalDotnet.Testing;

namespace TerminalDotnet.Explorer;

public enum ExplorerStatus
{
    Loading,
    Ready,
    Running,
    Failed
}

public enum TestNodeKind
{
    Project,
    Class,
    Test
}

public enum TestNodeUpdate
{
    Unchanged,
    Added,
    Edited
}

public enum TestNodeOutcome
{
    NotRun,
    Running,
    Passed,
    Skipped,
    Failed
}

public sealed record VisibleTestNode(
    int Depth,
    TestNodeKind Kind,
    string Name,
    IReadOnlyList<TestCase> Tests,
    TestNodeOutcome Outcome = TestNodeOutcome.NotRun,
    bool IsExpanded = true,
    TestNodeUpdate Update = TestNodeUpdate.Unchanged);

public sealed record ExplorerState(
    ExplorerStatus Status,
    IReadOnlyList<VisibleTestNode> VisibleNodes,
    int SelectedIndex,
    string Message,
    TestRun? LastRun = null,
    SourceLocation? SourceLocation = null,
    string SearchQuery = "",
    ExplorerFilter? ActiveFilter = null)
{
    /// <summary>What went wrong with the most recent attempt to run, when
    /// something did. It is kept apart from <see cref="LastRun"/> so a run
    /// that never produced results cannot hide behind an older one.</summary>
    public string? Diagnostic { get; init; }
}

public abstract record ExplorerCommand
{
    public sealed record Search(string Query) : ExplorerCommand;
    public sealed record ClearSearch : ExplorerCommand;
    public sealed record ToggleFilter(ExplorerFilter Filter) : ExplorerCommand;
    public sealed record ToggleExpanded : ExplorerCommand;
    public sealed record ToggleAllExpanded : ExplorerCommand;
    public sealed record MoveUp : ExplorerCommand;
    public sealed record MoveDown : ExplorerCommand;
    public sealed record RunSelected : ExplorerCommand;
    public sealed record RerunLast : ExplorerCommand;
    public sealed record RerunFailed : ExplorerCommand;
    public sealed record NextFailure : ExplorerCommand;
    public sealed record LoadSelectedSource : ExplorerCommand;
}
