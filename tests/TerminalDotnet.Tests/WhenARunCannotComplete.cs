using TerminalDotnet.Explorer;
using TerminalDotnet.Testing;
using TerminalDotnet.Tests.Builders;
using Xunit;

namespace TerminalDotnet.Tests.Testing;

public sealed class WhenARunCannotComplete
{
    [Fact]
    public async Task It_stops_reporting_the_tests_as_running()
    {
        // Arrange
        var session = await SessionRunningOneTestAsync(
            new BrokenTestBackend(CartTest, "dotnet test could not start"));

        // Act
        await session.DispatchAsync(new ExplorerCommand.RunSelected());

        // Assert
        Assert.NotEqual(ExplorerStatus.Running, session.State.Status);
    }

    [Fact]
    public async Task It_reports_why_the_run_did_not_happen()
    {
        // Arrange
        var session = await SessionRunningOneTestAsync(
            new BrokenTestBackend(CartTest, "dotnet test could not start"));

        // Act
        await session.DispatchAsync(new ExplorerCommand.RunSelected());

        // Assert
        Assert.Equal("dotnet test could not start", session.State.Message);
    }

    [Fact]
    public async Task It_leaves_the_tests_unrun()
    {
        // Arrange
        var session = await SessionRunningOneTestAsync(
            new BrokenTestBackend(CartTest, "dotnet test could not start"));

        // Act
        await session.DispatchAsync(new ExplorerCommand.RunSelected());

        // Assert
        Assert.Equal(TestNodeOutcome.NotRun, session.State.VisibleNodes[2].Outcome);
    }

    [Fact]
    public async Task It_reports_the_failure_apart_from_the_run_it_kept()
    {
        // Arrange
        var session = await SessionRunningOneTestAsync(
            new BrokenTestBackend(CartTest, "dotnet test could not start"));

        // Act
        await session.DispatchAsync(new ExplorerCommand.RunSelected());

        // Assert
        Assert.Equal("dotnet test could not start", session.State.Diagnostic);
    }

    [Fact]
    public async Task It_accepts_another_run_afterwards()
    {
        // Arrange
        var backend = new BrokenTestBackend(CartTest, "dotnet test could not start");
        var session = await SessionRunningOneTestAsync(backend);
        await session.DispatchAsync(new ExplorerCommand.RunSelected());

        // Act
        await session.DispatchAsync(new ExplorerCommand.RunSelected());

        // Assert
        Assert.Equal(2, backend.RunCount);
    }

    private static readonly TestCase CartTest =
        new("Shop.Tests.CartTests.Adds_item", "Adds item", "Shop.Tests.csproj");

    private static async Task<TestExplorerSession> SessionRunningOneTestAsync(ITestBackend backend)
    {
        var session = await GivenA.TestExplorer()
            .WithBackend(backend)
            .LoadedAsync();
        await session.DispatchAsync(new ExplorerCommand.MoveDown());
        await session.DispatchAsync(new ExplorerCommand.MoveDown());
        return session;
    }

    private sealed class BrokenTestBackend(TestCase test, string failure) : ITestBackend
    {
        public int RunCount { get; private set; }

        public Task<IReadOnlyList<TestCase>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TestCase>>([test]);

        public Task<TestRun> RunAsync(
            IReadOnlyCollection<TestCase> tests,
            CancellationToken cancellationToken = default)
        {
            RunCount++;
            return Task.FromException<TestRun>(new InvalidOperationException(failure));
        }
    }
}
