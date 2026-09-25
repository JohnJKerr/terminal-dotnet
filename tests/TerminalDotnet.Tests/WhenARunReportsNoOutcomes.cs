using TerminalDotnet.Explorer;
using TerminalDotnet.Testing;
using TerminalDotnet.Tests.Builders;
using Xunit;

namespace TerminalDotnet.Tests.Explorer;

public sealed class WhenARunReportsNoOutcomes
{
    [Fact]
    public async Task It_leaves_a_test_the_results_never_mentioned_unrun()
    {
        // Arrange
        var session = await SessionRunningOneTestAsync(new TestRun(true, "Build succeeded"));

        // Act
        await session.DispatchAsync(new ExplorerCommand.RunSelected());

        // Assert
        Assert.Equal(TestNodeOutcome.NotRun, session.State.VisibleNodes[2].Outcome);
    }

    [Fact]
    public async Task It_reports_why_the_results_are_missing()
    {
        // Arrange
        var session = await SessionRunningOneTestAsync(new TestRun(true, "Build succeeded")
        {
            Diagnostic = "Could not read the test results: No results were written."
        });

        // Act
        await session.DispatchAsync(new ExplorerCommand.RunSelected());

        // Assert
        Assert.Equal(
            "Could not read the test results: No results were written.",
            session.State.Diagnostic);
    }

    private static readonly TestCase CartTest =
        new("Shop.Tests.CartTests.Adds_item", "Adds item", "Shop.Tests.csproj");

    private static async Task<TestExplorerSession> SessionRunningOneTestAsync(TestRun run)
    {
        var session = await GivenA.TestExplorer()
            .WithBackend(new ResultlessTestBackend(CartTest, run))
            .LoadedAsync();
        await session.DispatchAsync(new ExplorerCommand.MoveDown());
        await session.DispatchAsync(new ExplorerCommand.MoveDown());
        return session;
    }

    private sealed class ResultlessTestBackend(TestCase test, TestRun run) : ITestBackend
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
