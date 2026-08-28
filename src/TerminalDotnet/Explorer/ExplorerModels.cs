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

public sealed record VisibleTestNode(
    int Depth,
    TestNodeKind Kind,
    string Name,
    IReadOnlyList<TestCase> Tests);

public sealed record ExplorerState(
    ExplorerStatus Status,
    IReadOnlyList<VisibleTestNode> VisibleNodes,
    int SelectedIndex,
    string Message);

public abstract record ExplorerCommand
{
    public sealed record MoveUp : ExplorerCommand;
    public sealed record MoveDown : ExplorerCommand;
    public sealed record RunSelected : ExplorerCommand;
}
