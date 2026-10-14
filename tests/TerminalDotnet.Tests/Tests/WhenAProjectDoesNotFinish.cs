using TerminalDotnet.Explorer;
using TerminalDotnet.Testing;
using TerminalDotnet.Tests.Builders;
using Xunit;

namespace TerminalDotnet.Tests.Tests;

public sealed class WhenAProjectDoesNotFinish
{
    private static readonly TestCase Cart =
        new("Shop.CartTests.Adds_item", "Adds item", "/repo/Cart.Tests.csproj");

    private static readonly TestCase Orders =
        new("Shop.OrderTests.Submits_order", "Submits order", "/repo/Orders.Tests.csproj");

    private static readonly TestCase Delivery =
        new("Shop.DeliveryTests.Ships_order", "Ships order", "/repo/Delivery.Tests.csproj");

    [Fact]
    public async Task It_names_the_project_that_did_not_finish()
    {
        // Arrange
        var session = await SessionAsync();

        // Act
        await RunAsync(session);

        // Assert
        Assert.Equal("Orders.Tests did not finish", session.State.Diagnostic);
    }

    [Fact]
    public async Task It_keeps_the_outcome_of_a_project_that_finished()
    {
        // Arrange
        var session = await SessionAsync();

        // Act
        await RunAsync(session);

        // Assert
        Assert.Equal(
            TestNodeOutcome.Passed,
            session.State.VisibleNodes.Single(node => node.Name == "Cart.Tests").Outcome);
    }

    [Fact]
    public async Task It_runs_the_projects_after_the_one_that_did_not_finish()
    {
        // Arrange
        var backend = new HoldingOneProject(Orders);
        var session = await SessionAsync(backend);

        // Act
        await RunAsync(session);

        // Assert
        Assert.Equal(
            [Cart.ProjectPath, Orders.ProjectPath, Delivery.ProjectPath],
            backend.ProjectsRan);
    }

    [Fact]
    public async Task It_cancels_the_project_that_did_not_finish()
    {
        // Arrange
        var backend = new HoldingOneProject(Orders);
        var session = await SessionAsync(backend);

        // Act
        await RunAsync(session);

        // Assert
        Assert.True(backend.HeldProjectWasCancelled);
    }

    private static Task<TestExplorerSession> SessionAsync(HoldingOneProject? backend = null) =>
        GivenA.TestExplorer()
            .WithBackend(backend ?? new HoldingOneProject(Orders))
            .FinishingAProjectWithin(TimeSpan.FromMilliseconds(30))
            .LoadedAsync();

    private static Task RunAsync(TestExplorerSession session) =>
        session.DispatchAsync(new ExplorerCommand.RunSelected()).WaitAsync(TimeSpan.FromSeconds(2));

    private sealed class HoldingOneProject(TestCase held) : ITestBackend
    {
        public List<string> ProjectsRan { get; } = [];

        public bool HeldProjectWasCancelled { get; private set; }

        public Task<IReadOnlyList<TestCase>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TestCase>>([Cart, Orders, Delivery]);

        public async Task<TestRun> RunAsync(
            IReadOnlyCollection<TestCase> tests,
            CancellationToken cancellationToken = default)
        {
            ProjectsRan.Add(tests.First().ProjectPath);
            if (tests.Any(test => test.ProjectPath == held.ProjectPath))
            {
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    HeldProjectWasCancelled = true;
                    throw;
                }
            }

            return new TestRun(
                true,
                "Passed",
                [.. tests.Select(test => new TestResult(test, TestOutcome.Passed, TimeSpan.Zero, null, null, null, null))]);
        }
    }
}
