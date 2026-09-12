using TerminalDotnet.Changes;
using TerminalDotnet.Filters;
using TerminalDotnet.Search;
using TerminalDotnet.Testing;

namespace TerminalDotnet.Explorer;

public sealed class TestExplorerSession(
    ITestBackend backend,
    ITestSourceLocator? testSourceLocator = null,
    IUpdatedSourceProvider? updatedSourceProvider = null)
{
    private readonly HashSet<string> collapsedNodes = [];
    private readonly Dictionary<TestCase, TestNodeOutcome> completedOutcomes = [];
    private readonly HashSet<TestCase> activeTests = [];
    private IReadOnlyList<TestCase> discoveredTests = [];
    private IReadOnlyList<TestCase> lastRunTests = [];
    private bool running;
    private IReadOnlyDictionary<string, TestNodeUpdate> updatedSuites =
        new Dictionary<string, TestNodeUpdate>(StringComparer.Ordinal);

    public ExplorerState State { get; private set; } =
        new(ExplorerStatus.Loading, [], 0, "Discovering tests...");

    public async Task LoadAsync(string target, CancellationToken cancellationToken = default)
    {
        try
        {
            State = await DiscoveredStateAsync(target, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            State = new ExplorerState(ExplorerStatus.Failed, [], 0, exception.Message);
        }
    }

    private async Task<ExplorerState> DiscoveredStateAsync(
        string target,
        CancellationToken cancellationToken)
    {
        var tests = Snapshot.Of(await backend.DiscoverAsync(target, cancellationToken));
        discoveredTests = tests;
        updatedSuites = await UpdatedSuitesAsync(target, cancellationToken);

        // The panels are open while discovery runs, so a search or filter
        // entered in the meantime survives the tests arriving.
        return State with
        {
            Status = ExplorerStatus.Ready,
            VisibleNodes = VisibleNodes(TestsMatching(State.SearchQuery, State.ActiveFilter)),
            SelectedIndex = 0,
            Message = $"Ready — {tests.Count} tests discovered"
        };
    }

    public Task DispatchAsync(ExplorerCommand command, CancellationToken cancellationToken = default) =>
        command switch
        {
            ExplorerCommand.LoadSelectedSource => LoadSelectedSourceAsync(cancellationToken),
            ExplorerCommand.Search search => Applied(() => ApplySearch(search.Query)),
            ExplorerCommand.ClearSearch => Applied(() => ApplySearch("")),
            ExplorerCommand.ToggleFilter filter => Applied(() => ApplyFilter(filter.Filter)),
            ExplorerCommand.ToggleExpanded => Applied(ToggleSelectedExpansion),
            ExplorerCommand.ToggleAllExpanded => Applied(ToggleWholeTreeExpansion),
            ExplorerCommand.RunSelected => RunSelectedAsync(cancellationToken),
            ExplorerCommand.RerunLast => RerunLastAsync(cancellationToken),
            ExplorerCommand.RerunFailed => RerunFailedAsync(cancellationToken),
            ExplorerCommand.NextFailure => Applied(() => SelectNext(FailedTestIndices())),
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
        State = State with { SourceLocation = source };
    }

    private async Task<IReadOnlyDictionary<string, TestNodeUpdate>> UpdatedSuitesAsync(
        string target,
        CancellationToken cancellationToken)
    {
        if (updatedSourceProvider is null)
        {
            return new Dictionary<string, TestNodeUpdate>(StringComparer.Ordinal);
        }

        var sources = await updatedSourceProvider.UpdatedSourcesAsync(target, cancellationToken);
        var projectDirectories = discoveredTests
            .Select(test => ProjectDirectoryOf(test.ProjectPath))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        return sources
            .SelectMany(source => SuiteUpdates(source, projectDirectories))
            .GroupBy(suite => suite.Key, StringComparer.Ordinal)
            .ToDictionary(
                suite => suite.Key,
                suite => suite.First().Update,
                StringComparer.Ordinal);
    }

    private static IEnumerable<(string Key, TestNodeUpdate Update)> SuiteUpdates(
        UpdatedSource source,
        IReadOnlyList<string> projectDirectories)
    {
        var project = ProjectHolding(source.Path, projectDirectories);
        return project is null
            ? []
            : [(SuiteKey(project, SuiteName(source.Path)), UpdateFrom(source.Change))];
    }

    private static string? ProjectHolding(string path, IReadOnlyList<string> projectDirectories) =>
        projectDirectories
            .Where(directory => path.StartsWith(
                directory + Path.DirectorySeparatorChar,
                StringComparison.Ordinal))
            .OrderByDescending(directory => directory.Length)
            .FirstOrDefault();

    private static string ProjectDirectoryOf(string projectPath) =>
        Path.GetDirectoryName(Path.GetFullPath(projectPath))!;

    private static string SuiteKey(string projectDirectory, string suiteName) =>
        $"{projectDirectory}:{suiteName}";

    private static string SuiteName(string path) => Path.GetFileNameWithoutExtension(path)!;

    private static TestNodeUpdate UpdateFrom(ChangeKind change) => change == ChangeKind.Added
        ? TestNodeUpdate.Added
        : TestNodeUpdate.Edited;

    private TestNodeUpdate UpdateOf(TestCase test) =>
        updatedSuites.GetValueOrDefault(SuiteKeyOf(test), TestNodeUpdate.Unchanged);

    private static string SuiteKeyOf(TestCase test) =>
        SuiteKey(ProjectDirectoryOf(test.ProjectPath), test.ClassName);

    private void ApplySearch(string query) => Show(query, State.ActiveFilter);

    private void ApplyFilter(ExplorerFilter filter) =>
        Show(State.SearchQuery, State.ActiveFilter == filter ? null : filter);

    private void Show(string query, ExplorerFilter? filter)
    {
        State = State with
        {
            VisibleNodes = VisibleNodes(TestsMatching(query, filter)),
            SelectedIndex = 0,
            SearchQuery = query,
            ActiveFilter = filter
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
        State = State with { VisibleNodes = CurrentNodes() };
    }

    private void ToggleWholeTreeExpansion()
    {
        var groupIds = GroupNodeIds(TestsMatching(State.SearchQuery, State.ActiveFilter));
        var collapseAll = groupIds.Any(id => !collapsedNodes.Contains(id));
        collapsedNodes.Clear();
        if (collapseAll)
        {
            collapsedNodes.UnionWith(groupIds);
        }

        var nodes = CurrentNodes();
        State = State with
        {
            VisibleNodes = nodes,
            SelectedIndex = Math.Min(State.SelectedIndex, Math.Max(0, nodes.Count - 1))
        };
    }

    private static IReadOnlyList<string> GroupNodeIds(IReadOnlyList<TestCase> tests) => tests
        .GroupBy(test => test.ProjectPath)
        .SelectMany(ProjectGroupIds)
        .ToArray();

    private static IEnumerable<string> ProjectGroupIds(IGrouping<string, TestCase> project) =>
    [
        NodeId(project.Key, TestNodeKind.Project, Path.GetFileNameWithoutExtension(project.Key)),
        .. project
            .Select(test => test.TestClass)
            .Distinct()
            .Select(testClass => NodeId(project.Key, TestNodeKind.Class, testClass))
    ];

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

    private void Select(int index)
    {
        if (index == SelectionRing.None)
        {
            return;
        }

        State = State with { SelectedIndex = index };
    }

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

    private IReadOnlyList<VisibleTestNode> VisibleNodes(IReadOnlyList<TestCase> tests) => Snapshot.Of(tests
        .GroupBy(test => test.ProjectPath)
        .OrderBy(project => project.Key)
        .SelectMany(ProjectNodes)
        .Select(NodeWithOutcome));

    private VisibleTestNode NodeWithOutcome(VisibleTestNode node)
    {
        if (node.Tests.All(activeTests.Contains))
        {
            return node with { Outcome = TestNodeOutcome.Running };
        }

        if (!node.Tests.All(completedOutcomes.ContainsKey))
        {
            return node;
        }

        return node with { Outcome = NodeOutcomeFrom(node.Tests.Select(test => completedOutcomes[test])) };
    }

    private IReadOnlyList<VisibleTestNode> CurrentNodes() =>
        VisibleNodes(TestsMatching(State.SearchQuery, State.ActiveFilter));

    private IReadOnlyList<TestCase> TestsMatching(string query, ExplorerFilter? filter) =>
        Snapshot.Of(discoveredTests
            .Where(test => MatchesSearch(test, query))
            .Where(test => PassesFilter(test, filter)));

    private bool PassesFilter(TestCase test, ExplorerFilter? filter) =>
        filter != ExplorerFilter.Updated || updatedSuites.ContainsKey(SuiteKeyOf(test));

    private static string NodeId(VisibleTestNode node) => NodeId(
        node.Tests[0].ProjectPath,
        node.Kind,
        node.Kind == TestNodeKind.Class ? node.Tests[0].TestClass : node.Name);

    private static string NodeId(string projectPath, TestNodeKind kind, string name) =>
        $"{projectPath}:{kind}:{name}";

    private static bool MatchesSearch(TestCase test, string query) =>
        SearchMatch.Matches(test.FullyQualifiedName, query) ||
        SearchMatch.Matches(test.DisplayName, query);

    private async Task RunTestsAsync(
        IReadOnlyList<TestCase> tests,
        CancellationToken cancellationToken)
    {
        if (running)
        {
            return;
        }

        running = true;
        try
        {
            await RunAdmittedTestsAsync(tests, cancellationToken);
        }
        finally
        {
            running = false;
        }
    }

    private async Task RunAdmittedTestsAsync(
        IReadOnlyList<TestCase> tests,
        CancellationToken cancellationToken)
    {
        lastRunTests = Snapshot.Of(tests);
        activeTests.Clear();
        activeTests.UnionWith(tests);
        State = State with
        {
            Status = ExplorerStatus.Running,
            VisibleNodes = CurrentNodes(),
            Message = $"Running {tests.Count} tests..."
        };
        TestRun run;
        try
        {
            run = await backend.RunAsync(tests, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            activeTests.Clear();
            State = State with
            {
                Status = ExplorerStatus.Ready,
                VisibleNodes = CurrentNodes(),
                Message = "Run cancelled",
                Diagnostic = "Run cancelled"
            };
            return;
        }
        catch (Exception exception)
        {
            activeTests.Clear();
            State = State with
            {
                Status = ExplorerStatus.Failed,
                VisibleNodes = CurrentNodes(),
                Message = exception.Message,
                Diagnostic = exception.Message
            };
            return;
        }

        foreach (var (test, outcome) in CompletedOutcomes(run))
        {
            completedOutcomes[test] = outcome;
        }

        activeTests.Clear();
        State = State with
        {
            Status = run.Passed ? ExplorerStatus.Ready : ExplorerStatus.Failed,
            VisibleNodes = CurrentNodes(),
            Message = run.Output,
            LastRun = run with { Results = Snapshot.Of(run.Results) },
            SourceLocation = FailureSourceFrom(run),
            Diagnostic = run.Diagnostic
        };
    }

    /// <summary>A test's outcome comes from its own result. An exit code
    /// covers the whole command, so a run that reported nothing leaves the
    /// tests it asked for unrun rather than passing or failing them all.
    /// </summary>
    private static IReadOnlyDictionary<TestCase, TestNodeOutcome> CompletedOutcomes(TestRun run) =>
        run.Results
            .GroupBy(result => result.Test)
            .ToDictionary(
                group => group.Key,
                group => NodeOutcomeFrom(group.Select(result => NodeOutcomeFor(result.Outcome))));

    private static SourceLocation? FailureSourceFrom(TestRun run)
    {
        var failure = run.Results.FirstOrDefault(result =>
            result.Outcome == TestOutcome.Failed &&
            result.SourceFile is not null &&
            result.SourceLine is not null);
        return failure is null ? null : new SourceLocation(failure.SourceFile!, failure.SourceLine!.Value);
    }

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
        var projectTests = Snapshot.Of(project);
        var projectNode = new VisibleTestNode(
            0,
            TestNodeKind.Project,
            Path.GetFileNameWithoutExtension(project.Key),
            projectTests);
        var projectCollapsed = collapsedNodes.Contains(NodeId(projectNode));
        var classNodes = projectTests
            .GroupBy(test => test.TestClass)
            .OrderBy(testClass => testClass.Key, StringComparer.Ordinal)
            .SelectMany(ClassNodes);

        return projectCollapsed
            ? [projectNode with { IsExpanded = false }]
            : [projectNode, .. classNodes];
    }

    private IEnumerable<VisibleTestNode> ClassNodes(IGrouping<string, TestCase> testClass)
    {
        var classTests = Snapshot.Of(testClass);
        var classNode = new VisibleTestNode(
            1,
            TestNodeKind.Class,
            classTests[0].ClassName,
            classTests,
            Update: UpdateOf(classTests[0]));
        var classCollapsed = collapsedNodes.Contains(NodeId(classNode));
        var testNodes = classTests
            .OrderBy(test => test.DisplayName)
            .Select(test => new VisibleTestNode(
                2,
                TestNodeKind.Test,
                test.DisplayName,
                [test],
                Update: UpdateOf(test)));

        return classCollapsed
            ? [classNode with { IsExpanded = false }]
            : [classNode, .. testNodes];
    }
}
