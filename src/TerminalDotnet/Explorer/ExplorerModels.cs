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
    SourceContext? SourceContext = null,
    string SearchQuery = "",
    ExplorerFilter? ActiveFilter = null);

public abstract record ExplorerCommand
{
    public sealed record Search(string Query) : ExplorerCommand;
    public sealed record ClearSearch : ExplorerCommand;
    public sealed record ToggleFilter(ExplorerFilter Filter) : ExplorerCommand;
    public sealed record ToggleExpanded : ExplorerCommand;
    public sealed record MoveUp : ExplorerCommand;
    public sealed record MoveDown : ExplorerCommand;
    public sealed record RunSelected : ExplorerCommand;
    public sealed record RerunLast : ExplorerCommand;
    public sealed record RerunFailed : ExplorerCommand;
    public sealed record NextFailure : ExplorerCommand;
    public sealed record NextSearchMatch : ExplorerCommand;
    public sealed record PreviousSearchMatch : ExplorerCommand;
    public sealed record LoadSelectedSource : ExplorerCommand;
}
