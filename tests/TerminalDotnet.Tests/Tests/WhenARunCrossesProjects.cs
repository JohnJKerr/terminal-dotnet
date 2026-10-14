using TerminalDotnet.Explorer;
using TerminalDotnet.Testing;
using TerminalDotnet.Tests.Builders;
using Xunit;

namespace TerminalDotnet.Tests.Tests;

public sealed class WhenARunCrossesProjects
{
    private static readonly TestCase Cart =
        new("Shop.CartTests.Adds_item", "Adds item", "/repo/Cart.Tests.csproj");

    private static readonly TestCase Orders =
        new("Shop.OrderTests.Submits_order", "Submits order", "/repo/Orders.Tests.csproj");

    [Fact]
    public async Task It_shows_a_finished_project_while_the_next_is_still_running()
    {
        // Arrange
        var backend = new SecondProjectHeld(Cart, Orders);
        var session = await GivenA.TestExplorer().WithBackend(backend).LoadedAsync();

        // Act
        var run = session.DispatchAsync(new ExplorerCommand.RunSelected());
        await backend.SecondProjectStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        // Assert
        Assert.Equal(
            TestNodeOutcome.Passed,
            session.State.VisibleNodes.Single(node => node.Name == "Cart.Tests").Outcome);

        backend.Finish();
        await run;
    }

    [Fact]
    public async Task It_names_the_project_that_is_still_running()
    {
        // Arrange
        var backend = new SecondProjectHeld(Cart, Orders);
        var session = await GivenA.TestExplorer().WithBackend(backend).LoadedAsync();

        // Act
        var run = session.DispatchAsync(new ExplorerCommand.RunSelected());
        await backend.SecondProjectStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        // Assert
        Assert.Equal("Running Orders.Tests (2 of 2)...", session.State.Message);

        backend.Finish();
        await run;
    }

    [Fact]
    public async Task It_shows_the_project_that_is_still_running()
    {
        // Arrange
        var backend = new SecondProjectHeld(Cart, Orders);
        var session = await GivenA.TestExplorer().WithBackend(backend).LoadedAsync();

        // Act
        var run = session.DispatchAsync(new ExplorerCommand.RunSelected());
        await backend.SecondProjectStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        // Assert
        Assert.Equal(
            TestNodeOutcome.Running,
            session.State.VisibleNodes.Single(node => node.Name == "Orders.Tests").Outcome);

        backend.Finish();
        await run;
    }

    private sealed class SecondProjectHeld(TestCase first, TestCase second) : ITestBackend
    {
        private readonly TaskCompletionSource finished = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource SecondProjectStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Finish() => finished.TrySetResult();

        public Task<IReadOnlyList<TestCase>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TestCase>>([first, second]);

        public Task<TestRun> RunAsync(
            IReadOnlyCollection<TestCase> tests,
            CancellationToken cancellationToken = default)
        {
            if (tests.Any(test => test.ProjectPath == second.ProjectPath))
            {
                SecondProjectStarted.TrySetResult();
                return Held(tests);
            }

            return Task.FromResult(Passed(tests));
        }

        private async Task<TestRun> Held(IReadOnlyCollection<TestCase> tests)
        {
            await finished.Task;
            return Passed(tests);
        }

        private static TestRun Passed(IReadOnlyCollection<TestCase> tests) => new(
            true,
            "Passed",
            [.. tests.Select(test => new TestResult(test, TestOutcome.Passed, TimeSpan.Zero, null, null, null, null))]);
    }
}
