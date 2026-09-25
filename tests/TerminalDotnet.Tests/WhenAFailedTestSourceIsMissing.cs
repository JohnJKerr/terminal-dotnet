using TerminalDotnet.Explorer;
using TerminalDotnet.Testing;
using TerminalDotnet.Tests.Builders;
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
        var test = GivenA.TestCase("Shop.Tests.CartTests.Adds_item");
        var failure = GivenA.ResultFor(test)
            .Failed("Expected total to be 10.")
            .At(Path.Combine(Path.GetTempPath(), $"terminal-dotnet-gone-{Guid.NewGuid():N}.cs"), 42)
            .Build();
        var session = await GivenA.TestExplorer()
            .WithBackend(new SingleRunBackend(test, new TestRun(false, "1 test failed", [failure])))
            .LoadedAsync();
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
