using TerminalDotnet.Explorer;
using TerminalDotnet.Testing;
using TerminalDotnet.Tests.Builders;
using Xunit;

namespace TerminalDotnet.Tests.Testing;

public sealed class WhenCancellingARun
{
    [Fact]
    public async Task It_is_ready_again_when_an_active_run_is_cancelled()
    {
        // Act
        var session = await CancelledRunAsync();

        // Assert
        Assert.Equal(ExplorerStatus.Ready, session.State.Status);
    }

    [Fact]
    public async Task It_reports_the_cancellation_when_an_active_run_is_cancelled()
    {
        // Act
        var session = await CancelledRunAsync();

        // Assert
        Assert.Equal("Run cancelled", session.State.Message);
    }

    [Fact]
    public async Task It_reports_the_cancellation_apart_from_the_run_it_kept()
    {
        // Act
        var session = await CancelledRunAsync();

        // Assert
        Assert.Equal("Run cancelled", session.State.Diagnostic);
    }

    [Fact]
    public async Task It_leaves_the_tests_unrun_when_an_active_run_is_cancelled()
    {
        // Act
        var session = await CancelledRunAsync();

        // Assert
        Assert.Equal(TestNodeOutcome.NotRun, session.State.VisibleNodes[2].Outcome);
    }

    private static async Task<TestExplorerSession> CancelledRunAsync()
    {
        var test = GivenA.TestCase("Shop.Tests.CartTests.Adds_item");
        var backend = new CancellableTestBackend(test);
        var session = await GivenA.TestExplorer()
            .WithBackend(backend)
            .LoadedAsync();
        await session.DispatchAsync(new ExplorerCommand.MoveDown());
        await session.DispatchAsync(new ExplorerCommand.MoveDown());
        using var cancellation = new CancellationTokenSource();
        var activeRun = session.DispatchAsync(new ExplorerCommand.RunSelected(), cancellation.Token);
        await backend.Started.Task.WaitAsync(TimeSpan.FromSeconds(1));
        await cancellation.CancelAsync();
        await activeRun;
        return session;
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
