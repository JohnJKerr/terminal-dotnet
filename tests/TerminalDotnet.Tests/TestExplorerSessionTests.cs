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
            (0, TestNodeKind.Project, "Shop.Tests"),
            (1, TestNodeKind.Class, "CartTests"),
            (2, TestNodeKind.Test, "Adding item updates total"),
            (2, TestNodeKind.Test, "Empty cart has zero total")
        ], session.State.VisibleNodes.Select(node => (node.Depth, node.Kind, node.Name)));
        Assert.Equal(ExplorerStatus.Ready, session.State.Status);
        Assert.Equal(0, session.State.SelectedIndex);
    }

    [Fact]
    public async Task Moving_selection_stays_within_the_visible_tree()
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

        // Act
        await session.DispatchAsync(new ExplorerCommand.MoveUp());

        // Assert
        Assert.Equal(1, session.State.SelectedIndex);
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
            "Shop.Tests.CartTests.Adds_item",
            "Shop.Tests.CartTests.Removes_item"
        ], backend.LastRun.Select(test => test.FullyQualifiedName));
        Assert.Equal(ExplorerStatus.Ready, session.State.Status);
        Assert.Equal("Passed", session.State.Message);
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
            TestNodeOutcome.Passed,
            TestNodeOutcome.Passed,
            TestNodeOutcome.Passed
        ], session.State.VisibleNodes.Skip(1).Take(3).Select(node => node.Outcome));
        Assert.Equal(TestNodeOutcome.NotRun, session.State.VisibleNodes[0].Outcome);
        Assert.Equal(TestNodeOutcome.NotRun, session.State.VisibleNodes[4].Outcome);
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
        Assert.Same(failure, Assert.Single(session.State.LastRun!.Results));
        Assert.Equal(TestNodeOutcome.Failed, session.State.VisibleNodes[2].Outcome);
    }

    private sealed class InMemoryTestBackend(
        IReadOnlyList<TestCase> tests,
        TestRun? run = null) : ITestBackend
    {
        public IReadOnlyCollection<TestCase> LastRun { get; private set; } = [];

        public Task<IReadOnlyList<TestCase>> DiscoverAsync(string target, CancellationToken cancellationToken = default) =>
            Task.FromResult(tests);

        public Task<TestRun> RunAsync(IReadOnlyCollection<TestCase> tests, CancellationToken cancellationToken = default)
        {
            LastRun = tests;
            return Task.FromResult(run ?? new TestRun(true, "Passed"));
        }
    }
}
