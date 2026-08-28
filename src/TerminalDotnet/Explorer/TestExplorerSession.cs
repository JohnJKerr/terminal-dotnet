using TerminalDotnet.Testing;

namespace TerminalDotnet.Explorer;

public sealed class TestExplorerSession(ITestBackend backend)
{
    public ExplorerState State { get; private set; } =
        new(ExplorerStatus.Loading, [], 0, "Discovering tests...");

    public async Task LoadAsync(string target, CancellationToken cancellationToken = default)
    {
        var tests = await backend.DiscoverAsync(target, cancellationToken);
        var nodes = tests
            .GroupBy(test => test.ProjectPath)
            .OrderBy(project => project.Key)
            .SelectMany(ProjectNodes)
            .ToArray();

        State = new ExplorerState(ExplorerStatus.Ready, nodes, 0, $"Ready — {tests.Count} tests discovered");
    }

    public async Task DispatchAsync(ExplorerCommand command, CancellationToken cancellationToken = default)
    {
        if (command is ExplorerCommand.RunSelected && State.VisibleNodes.Count > 0)
        {
            var selected = State.VisibleNodes[State.SelectedIndex];
            State = State with { Status = ExplorerStatus.Running, Message = $"Running {selected.Tests.Count} tests..." };
            var run = await backend.RunAsync(selected.Tests, cancellationToken);
            State = State with
            {
                Status = run.Passed ? ExplorerStatus.Ready : ExplorerStatus.Failed,
                Message = run.Output
            };
            return;
        }

        var lastIndex = Math.Max(0, State.VisibleNodes.Count - 1);
        var selectedIndex = command switch
        {
            ExplorerCommand.MoveUp => Math.Max(0, State.SelectedIndex - 1),
            ExplorerCommand.MoveDown => Math.Min(lastIndex, State.SelectedIndex + 1),
            _ => State.SelectedIndex
        };

        State = State with { SelectedIndex = selectedIndex };
    }

    private static string ClassName(string fullyQualifiedName)
    {
        var parts = fullyQualifiedName.Split('.');
        return parts.Length > 1 ? parts[^2] : fullyQualifiedName;
    }

    private static IEnumerable<VisibleTestNode> ProjectNodes(IGrouping<string, TestCase> project)
    {
        var projectTests = project.ToArray();
        var projectNode = new VisibleTestNode(
            0,
            TestNodeKind.Project,
            Path.GetFileNameWithoutExtension(project.Key),
            projectTests);
        var classNodes = projectTests
            .GroupBy(test => ClassName(test.FullyQualifiedName))
            .OrderBy(testClass => testClass.Key)
            .SelectMany(ClassNodes);

        return [projectNode, .. classNodes];
    }

    private static IEnumerable<VisibleTestNode> ClassNodes(IGrouping<string, TestCase> testClass)
    {
        var classTests = testClass.ToArray();
        var classNode = new VisibleTestNode(1, TestNodeKind.Class, testClass.Key, classTests);
        var testNodes = classTests
            .OrderBy(test => test.DisplayName)
            .Select(test => new VisibleTestNode(2, TestNodeKind.Test, test.DisplayName, [test]));

        return [classNode, .. testNodes];
    }
}
