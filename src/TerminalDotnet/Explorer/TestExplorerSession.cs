using TerminalDotnet.Search;
using TerminalDotnet.Testing;

namespace TerminalDotnet.Explorer;

public sealed class TestExplorerSession(
    ITestBackend backend,
    ISourceProvider? sourceProvider = null,
    ITestSourceLocator? testSourceLocator = null)
{
    private readonly HashSet<string> collapsedNodes = [];
    private readonly Dictionary<TestCase, TestNodeOutcome> completedOutcomes = [];
    private IReadOnlyList<TestCase> discoveredTests = [];
    private IReadOnlyList<TestCase> lastRunTests = [];

    public ExplorerState State { get; private set; } =
        new(ExplorerStatus.Loading, [], 0, "Discovering tests...");

    public async Task LoadAsync(string target, CancellationToken cancellationToken = default)
    {
        var tests = await backend.DiscoverAsync(target, cancellationToken);
        discoveredTests = tests;
        var nodes = VisibleNodes(tests);

        State = new ExplorerState(ExplorerStatus.Ready, nodes, 0, $"Ready — {tests.Count} tests discovered");
    }

    public Task DispatchAsync(ExplorerCommand command, CancellationToken cancellationToken = default) =>
        command switch
        {
            ExplorerCommand.LoadSelectedSource => LoadSelectedSourceAsync(cancellationToken),
            ExplorerCommand.Search search => Applied(() => ApplySearch(search.Query)),
            ExplorerCommand.ClearSearch => Applied(() => ApplySearch("")),
            ExplorerCommand.ToggleExpanded => Applied(ToggleSelectedExpansion),
            ExplorerCommand.RunSelected => RunSelectedAsync(cancellationToken),
            ExplorerCommand.RerunLast => RerunLastAsync(cancellationToken),
            ExplorerCommand.RerunFailed => RerunFailedAsync(cancellationToken),
            ExplorerCommand.NextFailure => Applied(() => SelectNext(FailedTestIndices())),
            ExplorerCommand.NextSearchMatch => Applied(() => SelectNext(TestIndices())),
            ExplorerCommand.PreviousSearchMatch => Applied(() => SelectPrevious(TestIndices())),
            _ => Applied(() => MoveSelection(command))
        };

    private static Task Applied(Action change)
    {
        change();
        return Task.CompletedTask;
    }

    private async Task LoadSelectedSourceAsync(CancellationToken cancellationToken)
    {
        if (State.VisibleNodes.Count == 0 || testSourceLocator is null)
        {
            return;
        }

        var selected = State.VisibleNodes[State.SelectedIndex];
        var source = await testSourceLocator.LocateAsync(selected.Tests[0], cancellationToken);
        State = State with { SourceContext = source };
    }

    private void ApplySearch(string query)
    {
        State = State with
        {
            VisibleNodes = VisibleNodes(TestsMatching(query)),
            SelectedIndex = 0,
            SearchQuery = query
        };
    }

    private void ToggleSelectedExpansion()
    {
        if (State.VisibleNodes.Count == 0)
        {
            return;
        }

        var selected = State.VisibleNodes[State.SelectedIndex];
        if (selected.Kind == TestNodeKind.Test)
        {
            return;
        }

        Collapse(selected, !selected.IsExpanded);
        State = State with { VisibleNodes = VisibleNodes(TestsMatching(State.SearchQuery)) };
    }

    private void Collapse(VisibleTestNode node, bool isExpanded)
    {
        if (isExpanded)
        {
            collapsedNodes.Remove(NodeId(node));
            return;
        }

        collapsedNodes.Add(NodeId(node));
    }

    private Task RunSelectedAsync(CancellationToken cancellationToken) => State.VisibleNodes.Count == 0
        ? Task.CompletedTask
        : RunTestsAsync(State.VisibleNodes[State.SelectedIndex].Tests, cancellationToken);

    private Task RerunLastAsync(CancellationToken cancellationToken) => lastRunTests.Count == 0
        ? Task.CompletedTask
        : RunTestsAsync(lastRunTests, cancellationToken);

    private Task RerunFailedAsync(CancellationToken cancellationToken)
    {
        var failedTests = FailedTests();
        return failedTests.Count == 0
            ? Task.CompletedTask
            : RunTestsAsync(failedTests, cancellationToken);
    }

    private void MoveSelection(ExplorerCommand command)
    {
        var lastIndex = Math.Max(0, State.VisibleNodes.Count - 1);
        State = State with
        {
            SelectedIndex = command switch
            {
                ExplorerCommand.MoveUp => Math.Max(0, State.SelectedIndex - 1),
                ExplorerCommand.MoveDown => Math.Min(lastIndex, State.SelectedIndex + 1),
                _ => State.SelectedIndex
            }
        };
    }

    private void SelectNext(IReadOnlyList<int> indices) =>
        Select(SelectionRing.Next(indices, State.SelectedIndex));

    private void SelectPrevious(IReadOnlyList<int> indices) =>
        Select(SelectionRing.Previous(indices, State.SelectedIndex));

    private void Select(int index)
    {
        if (index == SelectionRing.None)
        {
            return;
        }

        State = State with { SelectedIndex = index };
    }

    private IReadOnlyList<int> TestIndices() => IndicesOf(node => node.Kind == TestNodeKind.Test);

    private IReadOnlyList<int> FailedTestIndices()
    {
        var failedTests = FailedTests().ToHashSet();
        return IndicesOf(node =>
            node.Kind == TestNodeKind.Test &&
            node.Tests.Any(failedTests.Contains));
    }

    private IReadOnlyList<int> IndicesOf(Func<VisibleTestNode, bool> matches) => State.VisibleNodes
        .Select((node, index) => (node, index))
        .Where(item => matches(item.node))
        .Select(item => item.index)
        .ToArray();

    private IReadOnlyList<TestCase> FailedTests() => State.LastRun?.Results
        .Where(result => result.Outcome == TestOutcome.Failed)
        .Select(result => result.Test)
        .ToArray() ?? [];

    private IReadOnlyList<VisibleTestNode> VisibleNodes(IReadOnlyList<TestCase> tests) => tests
        .GroupBy(test => test.ProjectPath)
        .OrderBy(project => project.Key)
        .SelectMany(ProjectNodes)
        .Select(NodeWithStoredOutcome)
        .ToArray();

    private VisibleTestNode NodeWithStoredOutcome(VisibleTestNode node)
    {
        if (!node.Tests.All(completedOutcomes.ContainsKey))
        {
            return node;
        }

        return node with { Outcome = NodeOutcomeFrom(node.Tests.Select(test => completedOutcomes[test])) };
    }

    private IReadOnlyList<TestCase> TestsMatching(string query) => discoveredTests
        .Where(test => MatchesSearch(test, query))
        .ToArray();

    private static string NodeId(VisibleTestNode node) =>
        $"{node.Tests[0].ProjectPath}:{node.Kind}:{node.Name}";

    private static bool MatchesSearch(TestCase test, string query) =>
        FuzzyMatch.Matches(test.FullyQualifiedName, query) ||
        FuzzyMatch.Matches(test.DisplayName, query);

    private async Task RunTestsAsync(
        IReadOnlyList<TestCase> tests,
        CancellationToken cancellationToken)
    {
        lastRunTests = tests;
        State = State with
        {
            Status = ExplorerStatus.Running,
            VisibleNodes = WithOutcome(tests, TestNodeOutcome.Running),
            Message = $"Running {tests.Count} tests..."
        };
        TestRun run;
        try
        {
            run = await backend.RunAsync(tests, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            State = State with
            {
                Status = ExplorerStatus.Ready,
                VisibleNodes = WithOutcome(tests, TestNodeOutcome.NotRun),
                Message = "Run cancelled"
            };
            return;
        }

        var sourceContext = await ReadFailureSourceAsync(run, cancellationToken);
        foreach (var (test, outcome) in CompletedOutcomes(tests, run))
        {
            completedOutcomes[test] = outcome;
        }

        State = State with
        {
            Status = run.Passed ? ExplorerStatus.Ready : ExplorerStatus.Failed,
            VisibleNodes = WithRunOutcome(tests, run),
            Message = run.Output,
            LastRun = run,
            SourceContext = sourceContext
        };
    }

    private static IReadOnlyDictionary<TestCase, TestNodeOutcome> CompletedOutcomes(
        IReadOnlyList<TestCase> tests,
        TestRun run)
    {
        if (run.Results.Count == 0)
        {
            return tests.ToDictionary(test => test, _ => OutcomeFor(run));
        }

        return run.Results
            .GroupBy(result => result.Test)
            .ToDictionary(
                group => group.Key,
                group => NodeOutcomeFrom(group.Select(result => NodeOutcomeFor(result.Outcome))));
    }

    private async Task<SourceContext?> ReadFailureSourceAsync(
        TestRun run,
        CancellationToken cancellationToken)
    {
        var failure = run.Results.FirstOrDefault(result =>
            result.Outcome == TestOutcome.Failed &&
            result.SourceFile is not null &&
            result.SourceLine is not null);
        if (failure is null || sourceProvider is null)
        {
            return null;
        }

        return await sourceProvider.ReadAsync(
            failure.SourceFile!,
            failure.SourceLine!.Value,
            cancellationToken);
    }

    private IReadOnlyList<VisibleTestNode> WithOutcome(
        IReadOnlyList<TestCase> selectedTests,
        TestNodeOutcome outcome)
    {
        var selected = selectedTests.ToHashSet();
        return State.VisibleNodes
            .Select(node => node.Tests.All(selected.Contains) ? node with { Outcome = outcome } : node)
            .ToArray();
    }

    private IReadOnlyList<VisibleTestNode> WithRunOutcome(
        IReadOnlyList<TestCase> selectedTests,
        TestRun run)
    {
        if (run.Results.Count == 0)
        {
            return WithOutcome(selectedTests, OutcomeFor(run));
        }

        var results = run.Results
            .GroupBy(result => result.Test)
            .ToDictionary(
                group => group.Key,
                group => group.FirstOrDefault(result => result.Outcome == TestOutcome.Failed) ?? group.First());
        return State.VisibleNodes
            .Select(node => NodeWithResult(node, results))
            .ToArray();
    }

    private static VisibleTestNode NodeWithResult(
        VisibleTestNode node,
        IReadOnlyDictionary<TestCase, TestResult> results)
    {
        var nodeResults = node.Tests
            .Where(results.ContainsKey)
            .Select(test => results[test])
            .ToArray();
        if (nodeResults.Length != node.Tests.Count)
        {
            return node;
        }

        return node with
        {
            Outcome = NodeOutcomeFrom(nodeResults.Select(result => NodeOutcomeFor(result.Outcome)))
        };
    }

    private static TestNodeOutcome OutcomeFor(TestRun run) =>
        run.Passed ? TestNodeOutcome.Passed : TestNodeOutcome.Failed;

    private static TestNodeOutcome NodeOutcomeFor(TestOutcome outcome) => outcome switch
    {
        TestOutcome.Failed => TestNodeOutcome.Failed,
        TestOutcome.Skipped => TestNodeOutcome.Skipped,
        _ => TestNodeOutcome.Passed
    };

    private static TestNodeOutcome NodeOutcomeFrom(IEnumerable<TestNodeOutcome> outcomes)
    {
        var nodeOutcomes = outcomes.ToArray();
        if (nodeOutcomes.Contains(TestNodeOutcome.Failed))
        {
            return TestNodeOutcome.Failed;
        }

        return nodeOutcomes.All(outcome => outcome == TestNodeOutcome.Skipped)
            ? TestNodeOutcome.Skipped
            : TestNodeOutcome.Passed;
    }

    private IEnumerable<VisibleTestNode> ProjectNodes(IGrouping<string, TestCase> project)
    {
        var projectTests = project.ToArray();
        var projectNode = new VisibleTestNode(
            0,
            TestNodeKind.Project,
            Path.GetFileNameWithoutExtension(project.Key),
            projectTests);
        var projectCollapsed = collapsedNodes.Contains(NodeId(projectNode));
        var classNodes = projectTests
            .GroupBy(test => test.ClassName)
            .OrderBy(testClass => testClass.Key)
            .SelectMany(ClassNodes);

        return projectCollapsed
            ? [projectNode with { IsExpanded = false }]
            : [projectNode, .. classNodes];
    }

    private IEnumerable<VisibleTestNode> ClassNodes(IGrouping<string, TestCase> testClass)
    {
        var classTests = testClass.ToArray();
        var classNode = new VisibleTestNode(1, TestNodeKind.Class, testClass.Key, classTests);
        var classCollapsed = collapsedNodes.Contains(NodeId(classNode));
        var testNodes = classTests
            .OrderBy(test => test.DisplayName)
            .Select(test => new VisibleTestNode(2, TestNodeKind.Test, test.DisplayName, [test]));

        return classCollapsed
            ? [classNode with { IsExpanded = false }]
            : [classNode, .. testNodes];
    }
}
