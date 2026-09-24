using TerminalDotnet.Explorer;
using TerminalDotnet.Terminal;
using TerminalDotnet.Testing;
using Xunit;

namespace TerminalDotnet.Tests.Terminal;

public sealed class WhenARunFailsAfterAnEarlierRun
{
    [Fact]
    public async Task It_shows_the_retry_running_instead_of_the_previous_error()
    {
        // Arrange
        var pending = new TaskCompletionSource<TestRun>();
        var session = new TestExplorerSession(new RetryingBackend(pending.Task));
        await session.LoadAsync("/repo/Shop.sln");
        await session.DispatchAsync(new ExplorerCommand.RunSelected());

        // Act
        var retry = session.DispatchAsync(new ExplorerCommand.RerunLast());
        var snapshot = TestPanelSnapshot.From(session.State, "/repo/Shop.sln");
        pending.SetResult(Passing());
        await retry;

        // Assert
        Assert.Equal("Running 1 test...", snapshot.StatusLine);
    }

    private sealed class RetryingBackend(Task<TestRun> retry) : ITestBackend
    {
        private bool attempted;

        public Task<IReadOnlyList<TestCase>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TestCase>>([CartTest]);

        public Task<TestRun> RunAsync(
            IReadOnlyCollection<TestCase> tests,
            CancellationToken cancellationToken = default)
        {
            if (attempted)
            {
                return retry;
            }

            attempted = true;
            throw new InvalidOperationException("previous error");
        }
    }

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
        Assert.Equal("0 Failed, 1 Passed, 0 Skipped, 1 Total", snapshot.StatusLine);
    }

    [Fact]
    public async Task It_shows_the_output_of_a_run_that_reported_no_results()
    {
        // Arrange
        var session = await SessionAfterRunsAsync(Passing, Unreadable);

        // Act
        var snapshot = TestPanelSnapshot.From(session.State, "/repo/Shop.sln");

        // Assert
        Assert.Equal("error CS1002: ; expected", snapshot.SelectedOutput);
    }

    private static readonly TestCase CartTest =
        new("Shop.Tests.CartTests.Adds_item", "Adds item", "Shop.Tests.csproj");

    private static TestRun Passing() => new(
        true,
        "1 test passed",
        [new TestResult(CartTest, TestOutcome.Passed, TimeSpan.Zero, null, null, null, null)]);

    private static TestRun Unreadable() => new(false, "error CS1002: ; expected")
    {
        Diagnostic = "Could not read the test results: No results were written."
    };

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
