using TerminalDotnet.Explorer;
using TerminalDotnet.Testing;
using Xunit;

namespace TerminalDotnet.Tests.Testing;

public sealed class WhenAFailedTestSourceIsMissing
{
    [Fact]
    public async Task It_still_records_the_results_of_the_run()
    {
        // Arrange
        var session = await SessionWithAFailureInAMissingFileAsync();

        // Act
        var lastRun = session.State.LastRun;

        // Assert
        Assert.Equal([TestOutcome.Failed], lastRun?.Results.Select(result => result.Outcome));
    }

    [Fact]
    public async Task It_still_reports_the_output_of_the_run()
    {
        // Arrange
        var session = await SessionWithAFailureInAMissingFileAsync();

        // Act
        var message = session.State.Message;

        // Assert
        Assert.Equal("1 test failed", message);
    }

    private static async Task<TestExplorerSession> SessionWithAFailureInAMissingFileAsync()
    {
        var test = new TestCase("Shop.Tests.CartTests.Adds_item", "Adds item", "Shop.Tests.csproj");
        var failure = new TestResult(
            test,
            TestOutcome.Failed,
            TimeSpan.Zero,
            "Expected total to be 10.",
            null,
            Path.Combine(Path.GetTempPath(), $"terminal-dotnet-gone-{Guid.NewGuid():N}.cs"),
            42);
        var session = new TestExplorerSession(
            new SingleRunBackend(test, new TestRun(false, "1 test failed", [failure])));
        await session.LoadAsync("/repo/Shop.sln");
        await session.DispatchAsync(new ExplorerCommand.MoveDown());
        await session.DispatchAsync(new ExplorerCommand.MoveDown());
        await session.DispatchAsync(new ExplorerCommand.RunSelected());
        return session;
    }

    private sealed class SingleRunBackend(TestCase test, TestRun run) : ITestBackend
    {
        public Task<IReadOnlyList<TestCase>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TestCase>>([test]);

        public Task<TestRun> RunAsync(
            IReadOnlyCollection<TestCase> tests,
            CancellationToken cancellationToken = default) => Task.FromResult(run);
    }
}
