using TerminalDotnet.Explorer;
using TerminalDotnet.Testing;
using Xunit;

namespace TerminalDotnet.Tests;

public sealed class TestExplorerSessionTests
{
    [Fact]
    public async Task Loading_discovered_tests_produces_a_project_class_and_test_tree()
    {
        // Arrange
        var backend = new InMemoryTestBackend(
        [
            new TestCase("Shop.Tests.CartTests.Adding_item_updates_total", "Adding item updates total", "Shop.Tests.csproj"),
            new TestCase("Shop.Tests.CartTests.Empty_cart_has_zero_total", "Empty cart has zero total", "Shop.Tests.csproj")
        ]);
        var session = new TestExplorerSession(backend);

        // Act
        await session.LoadAsync("/repo/Shop.sln");

        // Assert
        Assert.Equal(
        [
            "node:0:Project:Shop.Tests",
            "node:1:Class:CartTests",
            "node:2:Test:Adding item updates total",
            "node:2:Test:Empty cart has zero total",
            "status:Ready",
            "selected:0"
        ],
            [.. session.State.VisibleNodes.Select(node => $"node:{node.Depth}:{node.Kind}:{node.Name}"),
                $"status:{session.State.Status}",
                $"selected:{session.State.SelectedIndex}"]);
    }

    [Fact]
    public async Task Moving_down_stops_at_the_end_of_the_visible_tree()
    {
        // Arrange
        var session = new TestExplorerSession(new InMemoryTestBackend(
        [
            new TestCase("Shop.Tests.CartTests.Adds_item", "Adds item", "Shop.Tests.csproj")
        ]));
        await session.LoadAsync("/repo/Shop.sln");

        // Act
        await session.DispatchAsync(new ExplorerCommand.MoveDown());
        await session.DispatchAsync(new ExplorerCommand.MoveDown());
        await session.DispatchAsync(new ExplorerCommand.MoveDown());

        // Assert
        Assert.Equal(2, session.State.SelectedIndex);
    }

    [Fact]
    public async Task Moving_up_selects_the_previous_visible_node()
    {
        // Arrange
        var session = new TestExplorerSession(new InMemoryTestBackend(
        [
            new TestCase("Shop.Tests.CartTests.Adds_item", "Adds item", "Shop.Tests.csproj")
        ]));
        await session.LoadAsync("/repo/Shop.sln");
        await session.DispatchAsync(new ExplorerCommand.MoveDown());
        await session.DispatchAsync(new ExplorerCommand.MoveDown());

        // Act
        await session.DispatchAsync(new ExplorerCommand.MoveUp());

        // Assert
        Assert.Equal(1, session.State.SelectedIndex);
    }

    [Fact]
    public async Task Searching_tests_keeps_case_insensitive_matches_with_their_ancestors()
    {
        // Arrange
        var session = new TestExplorerSession(new InMemoryTestBackend(
        [
            new TestCase("Shop.Tests.CartTests.Adds_item", "Adds item", "Shop.Tests.csproj"),
            new TestCase("Shop.Tests.OrderTests.Submits_order", "Submits order", "Shop.Tests.csproj")
        ]));
        await session.LoadAsync("/repo/Shop.sln");

        // Act
        await session.DispatchAsync(new ExplorerCommand.Search("ORDER"));

        // Assert
        Assert.Equal(
        [
            "node:0:Project:Shop.Tests",
            "node:1:Class:OrderTests",
            "node:2:Test:Submits order"
        ], session.State.VisibleNodes.Select(node => $"node:{node.Depth}:{node.Kind}:{node.Name}"));
    }

    [Fact]
    public async Task Clearing_search_restores_the_test_tree_with_a_valid_selection()
    {
        // Arrange
        var session = new TestExplorerSession(new InMemoryTestBackend(
        [
            new TestCase("Shop.Tests.CartTests.Adds_item", "Adds item", "Shop.Tests.csproj"),
            new TestCase("Shop.Tests.OrderTests.Submits_order", "Submits order", "Shop.Tests.csproj")
        ]));
        await session.LoadAsync("/repo/Shop.sln");
        await session.DispatchAsync(new ExplorerCommand.Search("missing"));

        // Act
        await session.DispatchAsync(new ExplorerCommand.ClearSearch());

        // Assert
        Assert.Equal(
            (5, 0),
            (session.State.VisibleNodes.Count, session.State.SelectedIndex));
    }

    [Fact]
    public async Task Searching_exposes_the_active_query()
    {
        // Arrange
        var session = new TestExplorerSession(new InMemoryTestBackend(
        [
            new TestCase("Shop.Tests.CartTests.Adds_item", "Adds item", "Shop.Tests.csproj")
        ]));
        await session.LoadAsync("/repo/Shop.sln");

        // Act
        await session.DispatchAsync(new ExplorerCommand.Search("cart"));

        // Assert
        Assert.Equal("cart", session.State.SearchQuery);
    }

    [Fact]
    public async Task Searching_tests_matches_the_displayed_test_name()
    {
        // Arrange
        var session = new TestExplorerSession(new InMemoryTestBackend(
        [
            new TestCase("Shop.Tests.CartTests.Empty_cart", "Empty cart has zero total", "Shop.Tests.csproj")
        ]));
        await session.LoadAsync("/repo/Shop.sln");

        // Act
        await session.DispatchAsync(new ExplorerCommand.Search("zero total"));

        // Assert
        Assert.Equal("Empty cart has zero total", session.State.VisibleNodes[2].Name);
    }

    [Fact]
    public async Task Collapsing_a_class_hides_its_tests()
    {
        // Arrange
        var session = new TestExplorerSession(new InMemoryTestBackend(
        [
            new TestCase("Shop.Tests.CartTests.Adds_item", "Adds item", "Shop.Tests.csproj"),
            new TestCase("Shop.Tests.OrderTests.Submits_order", "Submits order", "Shop.Tests.csproj")
        ]));
        await session.LoadAsync("/repo/Shop.sln");
        await session.DispatchAsync(new ExplorerCommand.MoveDown());

        // Act
        await session.DispatchAsync(new ExplorerCommand.ToggleExpanded());

        // Assert
        Assert.Equal(
        [
            "Project:Shop.Tests",
            "Class:CartTests",
            "Class:OrderTests",
            "Test:Submits order"
        ], session.State.VisibleNodes.Select(node => $"{node.Kind}:{node.Name}"));
    }

    [Fact]
    public async Task Expanding_a_collapsed_class_restores_its_tests()
    {
        // Arrange
        var session = new TestExplorerSession(new InMemoryTestBackend(
        [
            new TestCase("Shop.Tests.CartTests.Adds_item", "Adds item", "Shop.Tests.csproj")
        ]));
        await session.LoadAsync("/repo/Shop.sln");
        await session.DispatchAsync(new ExplorerCommand.MoveDown());
        await session.DispatchAsync(new ExplorerCommand.ToggleExpanded());

        // Act
        await session.DispatchAsync(new ExplorerCommand.ToggleExpanded());

        // Assert
        Assert.Equal(
        ["Project:Shop.Tests", "Class:CartTests", "Test:Adds item"],
            session.State.VisibleNodes.Select(node => $"{node.Kind}:{node.Name}"));
    }

    [Fact]
    public async Task Running_a_class_runs_every_test_beneath_the_selected_class()
    {
        // Arrange
        var backend = new InMemoryTestBackend(
        [
            new TestCase("Shop.Tests.CartTests.Adds_item", "Adds item", "Shop.Tests.csproj"),
            new TestCase("Shop.Tests.CartTests.Removes_item", "Removes item", "Shop.Tests.csproj"),
            new TestCase("Shop.Tests.OrderTests.Submits_order", "Submits order", "Shop.Tests.csproj")
        ]);
        var session = new TestExplorerSession(backend);
        await session.LoadAsync("/repo/Shop.sln");
        await session.DispatchAsync(new ExplorerCommand.MoveDown());

        // Act
        await session.DispatchAsync(new ExplorerCommand.RunSelected());

        // Assert
        Assert.Equal(
        [
            "test:Shop.Tests.CartTests.Adds_item",
            "test:Shop.Tests.CartTests.Removes_item",
            "status:Ready",
            "message:Passed"
        ],
            [.. backend.LastRun.Select(test => $"test:{test.FullyQualifiedName}"),
                $"status:{session.State.Status}",
                $"message:{session.State.Message}"]);
    }

    [Fact]
    public async Task A_completed_run_retains_outcomes_on_the_selected_subtree()
    {
        // Arrange
        var backend = new InMemoryTestBackend(
        [
            new TestCase("Shop.Tests.CartTests.Adds_item", "Adds item", "Shop.Tests.csproj"),
            new TestCase("Shop.Tests.CartTests.Removes_item", "Removes item", "Shop.Tests.csproj"),
            new TestCase("Shop.Tests.OrderTests.Submits_order", "Submits order", "Shop.Tests.csproj")
        ]);
        var session = new TestExplorerSession(backend);
        await session.LoadAsync("/repo/Shop.sln");
        await session.DispatchAsync(new ExplorerCommand.MoveDown());

        // Act
        await session.DispatchAsync(new ExplorerCommand.RunSelected());

        // Assert
        Assert.Equal(
        [
            TestNodeOutcome.NotRun,
            TestNodeOutcome.Passed,
            TestNodeOutcome.Passed,
            TestNodeOutcome.Passed,
            TestNodeOutcome.NotRun,
            TestNodeOutcome.NotRun
        ], session.State.VisibleNodes.Select(node => node.Outcome));
    }

    [Fact]
    public async Task A_failed_run_exposes_the_selected_tests_failure_details()
    {
        // Arrange
        var test = new TestCase("Shop.Tests.CartTests.Adds_item", "Adds item", "Shop.Tests.csproj");
        var failure = new TestResult(
            test,
            TestOutcome.Failed,
            TimeSpan.FromMilliseconds(12),
            "Expected total to be 10.",
            "at CartTests.Adds_item() in /repo/CartTests.cs:line 42",
            "/repo/CartTests.cs",
            42);
        var backend = new InMemoryTestBackend([test], new TestRun(false, "1 test failed", [failure]));
        var session = new TestExplorerSession(backend);
        await session.LoadAsync("/repo/Shop.sln");
        await session.DispatchAsync(new ExplorerCommand.MoveDown());
        await session.DispatchAsync(new ExplorerCommand.MoveDown());

        // Act
        await session.DispatchAsync(new ExplorerCommand.RunSelected());

        // Assert
        Assert.Equal(
            (failure, TestNodeOutcome.Failed),
            (session.State.LastRun!.Results.Single(), session.State.VisibleNodes[2].Outcome));
    }

    [Fact]
    public async Task A_failed_run_loads_source_context_around_the_failure_line()
    {
        // Arrange
        var test = new TestCase("Shop.Tests.CartTests.Adds_item", "Adds item", "Shop.Tests.csproj");
        var failure = new TestResult(
            test,
            TestOutcome.Failed,
            TimeSpan.Zero,
            "Expected total to be 10.",
            null,
            "/repo/CartTests.cs",
            42);
        var source = new SourceContext(
            "/repo/CartTests.cs",
            41,
            42,
            ["var cart = new Cart();", "Assert.Equal(10, cart.Total);"]);
        var session = new TestExplorerSession(
            new InMemoryTestBackend([test], new TestRun(false, "1 test failed", [failure])),
            new InMemorySourceProvider(source));
        await session.LoadAsync("/repo/Shop.sln");
        await session.DispatchAsync(new ExplorerCommand.MoveDown());
        await session.DispatchAsync(new ExplorerCommand.MoveDown());

        // Act
        await session.DispatchAsync(new ExplorerCommand.RunSelected());

        // Assert
        Assert.Same(source, session.State.SourceContext);
    }

    [Fact]
    public async Task Moving_to_the_next_failure_selects_the_first_failed_test()
    {
        // Arrange
        var first = new TestCase("Shop.Tests.CartTests.Adds_item", "Adds item", "Shop.Tests.csproj");
        var second = new TestCase("Shop.Tests.CartTests.Removes_item", "Removes item", "Shop.Tests.csproj");
        var run = new TestRun(false, "2 tests failed",
        [
            new TestResult(first, TestOutcome.Failed, TimeSpan.Zero, "Failed", null, null, null),
            new TestResult(second, TestOutcome.Failed, TimeSpan.Zero, "Failed", null, null, null)
        ]);
        var session = new TestExplorerSession(new InMemoryTestBackend([first, second], run));
        await session.LoadAsync("/repo/Shop.sln");
        await session.DispatchAsync(new ExplorerCommand.RunSelected());

        // Act
        await session.DispatchAsync(new ExplorerCommand.NextFailure());

        // Assert
        Assert.Equal("Adds item", session.State.VisibleNodes[session.State.SelectedIndex].Name);
    }

    [Fact]
    public async Task Rerunning_the_last_run_uses_the_previous_tests_after_selection_moves()
    {
        // Arrange
        var backend = new InMemoryTestBackend(
        [
            new TestCase("Shop.Tests.CartTests.Adds_item", "Adds item", "Shop.Tests.csproj"),
            new TestCase("Shop.Tests.CartTests.Removes_item", "Removes item", "Shop.Tests.csproj")
        ]);
        var session = new TestExplorerSession(backend);
        await session.LoadAsync("/repo/Shop.sln");
        await session.DispatchAsync(new ExplorerCommand.MoveDown());
        await session.DispatchAsync(new ExplorerCommand.MoveDown());
        await session.DispatchAsync(new ExplorerCommand.RunSelected());
        await session.DispatchAsync(new ExplorerCommand.MoveDown());

        // Act
        await session.DispatchAsync(new ExplorerCommand.RerunLast());

        // Assert
        Assert.Equal(
        [
            "Shop.Tests.CartTests.Adds_item",
            "Shop.Tests.CartTests.Adds_item"
        ], backend.RunHistory.SelectMany(run => run).Select(test => test.FullyQualifiedName));
    }

    [Fact]
    public async Task Rerunning_failures_runs_only_tests_that_failed_in_the_previous_run()
    {
        // Arrange
        var failed = new TestCase("Shop.Tests.CartTests.Adds_item", "Adds item", "Shop.Tests.csproj");
        var passed = new TestCase("Shop.Tests.CartTests.Removes_item", "Removes item", "Shop.Tests.csproj");
        var failure = new TestResult(failed, TestOutcome.Failed, TimeSpan.Zero, "Failed", null, null, null);
        var backend = new InMemoryTestBackend(
            [failed, passed],
            new TestRun(false, "1 test failed", [failure]));
        var session = new TestExplorerSession(backend);
        await session.LoadAsync("/repo/Shop.sln");
        await session.DispatchAsync(new ExplorerCommand.MoveDown());
        await session.DispatchAsync(new ExplorerCommand.RunSelected());

        // Act
        await session.DispatchAsync(new ExplorerCommand.RerunFailed());

        // Assert
        Assert.Equal(
        [
            "run:Shop.Tests.CartTests.Adds_item,Shop.Tests.CartTests.Removes_item",
            "run:Shop.Tests.CartTests.Adds_item"
        ], backend.RunHistory.Select(run => $"run:{string.Join(',', run.Select(test => test.FullyQualifiedName))}"));
    }

    [Fact]
    public async Task Cancelling_an_active_run_restores_a_ready_not_run_state()
    {
        // Arrange
        var test = new TestCase("Shop.Tests.CartTests.Adds_item", "Adds item", "Shop.Tests.csproj");
        var backend = new CancellableTestBackend(test);
        var session = new TestExplorerSession(backend);
        await session.LoadAsync("/repo/Shop.sln");
        await session.DispatchAsync(new ExplorerCommand.MoveDown());
        await session.DispatchAsync(new ExplorerCommand.MoveDown());
        using var cancellation = new CancellationTokenSource();
        var activeRun = session.DispatchAsync(new ExplorerCommand.RunSelected(), cancellation.Token);
        await backend.Started.Task.WaitAsync(TimeSpan.FromSeconds(1));

        // Act
        await cancellation.CancelAsync();
        await activeRun;

        // Assert
        Assert.Equal(
            (ExplorerStatus.Ready, "Run cancelled", TestNodeOutcome.NotRun),
            (session.State.Status, session.State.Message, session.State.VisibleNodes[2].Outcome));
    }

    private sealed class InMemoryTestBackend(
        IReadOnlyList<TestCase> tests,
        TestRun? run = null) : ITestBackend
    {
        public IReadOnlyCollection<TestCase> LastRun { get; private set; } = [];
        public List<IReadOnlyCollection<TestCase>> RunHistory { get; } = [];

        public Task<IReadOnlyList<TestCase>> DiscoverAsync(string target, CancellationToken cancellationToken = default) =>
            Task.FromResult(tests);

        public Task<TestRun> RunAsync(IReadOnlyCollection<TestCase> tests, CancellationToken cancellationToken = default)
        {
            LastRun = tests;
            RunHistory.Add(tests);
            return Task.FromResult(run ?? new TestRun(true, "Passed"));
        }
    }

    private sealed class InMemorySourceProvider(SourceContext source) : ISourceProvider
    {
        public Task<SourceContext> ReadAsync(
            string path,
            int line,
            CancellationToken cancellationToken = default) => Task.FromResult(source);
    }

    private sealed class CancellableTestBackend(TestCase test) : ITestBackend
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<IReadOnlyList<TestCase>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<TestCase>>([test]);

        public async Task<TestRun> RunAsync(
            IReadOnlyCollection<TestCase> tests,
            CancellationToken cancellationToken = default)
        {
            Started.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new TestRun(true, "Passed");
        }
    }
}
