using TerminalDotnet.Testing;

namespace TerminalDotnet.Explorer;

public sealed class TestExplorerSession(ITestBackend backend, ISourceProvider? sourceProvider = null)
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

    public async Task DispatchAsync(ExplorerCommand command, CancellationToken cancellationToken = default)
    {
        if (command is ExplorerCommand.Search search)
        {
            var matchingTests = discoveredTests
                .Where(test => MatchesSearch(test, search.Query))
                .ToArray();
            State = State with
            {
                VisibleNodes = VisibleNodes(matchingTests),
                SelectedIndex = 0,
                SearchQuery = search.Query
            };
            return;
        }

        if (command is ExplorerCommand.ClearSearch)
        {
            State = State with
            {
                VisibleNodes = VisibleNodes(discoveredTests),
                SelectedIndex = 0,
                SearchQuery = ""
            };
            return;
        }

        if (command is ExplorerCommand.ToggleExpanded && State.VisibleNodes.Count > 0)
        {
            var selected = State.VisibleNodes[State.SelectedIndex];
            if (selected.Kind != TestNodeKind.Test)
            {
                if (selected.IsExpanded)
                {
                    collapsedNodes.Add(NodeId(selected));
                }
                else
                {
                    collapsedNodes.Remove(NodeId(selected));
                }

                State = State with { VisibleNodes = VisibleNodes(TestsForSearch(State.SearchQuery)) };
            }

            return;
        }

        if (command is ExplorerCommand.RunSelected && State.VisibleNodes.Count > 0)
        {
            await RunTestsAsync(State.VisibleNodes[State.SelectedIndex].Tests, cancellationToken);
            return;
        }

        if (command is ExplorerCommand.RerunLast && lastRunTests.Count > 0)
        {
            await RunTestsAsync(lastRunTests, cancellationToken);
            return;
        }

        if (command is ExplorerCommand.RerunFailed)
        {
            var failedTests = State.LastRun?.Results
                .Where(result => result.Outcome == TestOutcome.Failed)
                .Select(result => result.Test)
                .ToArray() ?? [];
            if (failedTests.Length > 0)
            {
                await RunTestsAsync(failedTests, cancellationToken);
            }

            return;
        }

        if (command is ExplorerCommand.NextFailure)
        {
            var failedTests = State.LastRun?.Results
                .Where(result => result.Outcome == TestOutcome.Failed)
                .Select(result => result.Test)
                .ToHashSet() ?? [];
            var failureIndices = State.VisibleNodes
                .Select((node, index) => (node, index))
                .Where(item =>
                    item.node.Kind == TestNodeKind.Test &&
                    item.node.Tests.Any(failedTests.Contains))
                .Select(item => item.index)
                .ToArray();
            var nextFailure = failureIndices.FirstOrDefault(
                index => index > State.SelectedIndex,
                failureIndices.FirstOrDefault(-1));
            if (nextFailure >= 0)
            {
                State = State with { SelectedIndex = nextFailure };
            }

            return;
        }

        if (command is ExplorerCommand.NextSearchMatch)
        {
            var matchIndices = State.VisibleNodes
                .Select((node, index) => (node, index))
                .Where(item => item.node.Kind == TestNodeKind.Test)
                .Select(item => item.index)
                .ToArray();
            var nextMatch = matchIndices.FirstOrDefault(
                index => index > State.SelectedIndex,
                matchIndices.FirstOrDefault(-1));
            if (nextMatch >= 0)
            {
                State = State with { SelectedIndex = nextMatch };
            }

            return;
        }

        if (command is ExplorerCommand.PreviousSearchMatch)
        {
            var previousMatch = State.VisibleNodes
                .Select((node, index) => (node, index))
                .LastOrDefault(item =>
                    item.index < State.SelectedIndex &&
                    item.node.Kind == TestNodeKind.Test);
            if (previousMatch.node is not null)
            {
                State = State with { SelectedIndex = previousMatch.index };
            }

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

        var outcomes = node.Tests.Select(test => completedOutcomes[test]).ToArray();
        var outcome = outcomes.Contains(TestNodeOutcome.Failed)
            ? TestNodeOutcome.Failed
            : outcomes.All(result => result == TestNodeOutcome.Skipped)
                ? TestNodeOutcome.Skipped
                : TestNodeOutcome.Passed;
        return node with { Outcome = outcome };
    }

    private IReadOnlyList<TestCase> TestsForSearch(string query) => discoveredTests
        .Where(test => MatchesSearch(test, query))
        .ToArray();

    private static string NodeId(VisibleTestNode node) =>
        $"{node.Tests[0].ProjectPath}:{node.Kind}:{node.Name}";

    private static bool MatchesSearch(TestCase test, string query) =>
        IsOrderedMatch(test.FullyQualifiedName, query) ||
        IsOrderedMatch(test.DisplayName, query);

    private static bool IsOrderedMatch(string candidate, string query)
    {
        var queryIndex = 0;
        foreach (var character in candidate)
        {
            if (queryIndex < query.Length &&
                char.ToUpperInvariant(character) == char.ToUpperInvariant(query[queryIndex]))
            {
                queryIndex++;
            }
        }

        return queryIndex == query.Length;
    }

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
            return tests.ToDictionary(
                test => test,
                _ => run.Passed ? TestNodeOutcome.Passed : TestNodeOutcome.Failed);
        }

        return run.Results
            .GroupBy(result => result.Test)
            .ToDictionary(
                group => group.Key,
                group => group.Any(result => result.Outcome == TestOutcome.Failed)
                    ? TestNodeOutcome.Failed
                    : group.All(result => result.Outcome == TestOutcome.Skipped)
                        ? TestNodeOutcome.Skipped
                        : TestNodeOutcome.Passed);
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

    private static string ClassName(string fullyQualifiedName)
    {
        var parts = fullyQualifiedName.Split('.');
        return parts.Length > 1 ? parts[^2] : fullyQualifiedName;
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
            return WithOutcome(
                selectedTests,
                run.Passed ? TestNodeOutcome.Passed : TestNodeOutcome.Failed);
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

        var outcome = nodeResults.Any(result => result.Outcome == TestOutcome.Failed)
            ? TestNodeOutcome.Failed
            : nodeResults.All(result => result.Outcome == TestOutcome.Skipped)
                ? TestNodeOutcome.Skipped
                : TestNodeOutcome.Passed;
        return node with { Outcome = outcome };
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
            .GroupBy(test => ClassName(test.FullyQualifiedName))
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
