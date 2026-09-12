using TerminalDotnet.Explorer;
using TerminalDotnet.Terminal;
using TerminalDotnet.Testing;
using Xunit;

namespace TerminalDotnet.Tests.Terminal;

public sealed class WhenARunFailsAfterAnEarlierRun
{
    [Fact]
    public async Task It_shows_why_the_latest_run_failed()
    {
        // Arrange
        var session = await SessionAfterRunsAsync(Passing, Broken);

        // Act
        var snapshot = TestPanelSnapshot.From(session.State, "/repo/Shop.sln");

        // Assert
        Assert.Equal("dotnet test could not start", snapshot.StatusLine);
    }

    [Fact]
    public async Task It_shows_the_failure_in_place_of_the_earlier_output()
    {
        // Arrange
        var session = await SessionAfterRunsAsync(Passing, Broken);

        // Act
        var snapshot = TestPanelSnapshot.From(session.State, "/repo/Shop.sln");

        // Assert
        Assert.Equal("dotnet test could not start", snapshot.SelectedOutput);
    }

    [Fact]
    public async Task It_summarizes_the_run_again_once_one_completes()
    {
        // Arrange
        var session = await SessionAfterRunsAsync(Passing, Broken, Passing);

        // Act
        var snapshot = TestPanelSnapshot.From(session.State, "/repo/Shop.sln");

        // Assert
        Assert.Equal("0 Failed, 1 Passed, 0 Skipped", snapshot.StatusLine);
    }

    private static readonly TestCase CartTest =
        new("Shop.Tests.CartTests.Adds_item", "Adds item", "Shop.Tests.csproj");

    private static TestRun Passing() => new(
        true,
        "1 test passed",
        [new TestResult(CartTest, TestOutcome.Passed, TimeSpan.Zero, null, null, null, null)]);

    private static TestRun Broken() =>
        throw new InvalidOperationException("dotnet test could not start");

    private static async Task<TestExplorerSession> SessionAfterRunsAsync(params Func<TestRun>[] runs)
    {
        var session = new TestExplorerSession(new ScriptedTestBackend(CartTest, runs));
        await session.LoadAsync("/repo/Shop.sln");
        await session.DispatchAsync(new ExplorerCommand.MoveDown());
        await session.DispatchAsync(new ExplorerCommand.MoveDown());
        foreach (var _ in runs)
        {
            await session.DispatchAsync(new ExplorerCommand.RunSelected());
        }

        return session;
    }

    private sealed class ScriptedTestBackend(TestCase test, IReadOnlyList<Func<TestRun>> runs)
        : ITestBackend
    {
        private int completed;

        public Task<IReadOnlyList<TestCase>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TestCase>>([test]);

        public Task<TestRun> RunAsync(
            IReadOnlyCollection<TestCase> tests,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(runs[Math.Min(completed++, runs.Count - 1)]());
    }
}
