using TerminalDotnet.Explorer;
using TerminalDotnet.Testing;
using Xunit;

namespace TerminalDotnet.Tests.Testing;

public sealed class WhenARunIsAlreadyActive
{
    [Fact]
    public async Task It_does_not_start_a_second_run()
    {
        // Arrange
        var backend = new HeldTestBackend(CartTest);
        var session = await SessionRunningOneTestAsync(backend);
        var active = session.DispatchAsync(new ExplorerCommand.RunSelected());
        await backend.Started.Task.WaitAsync(TimeSpan.FromSeconds(1));

        // Act
        await session.DispatchAsync(new ExplorerCommand.RunSelected())
            .WaitAsync(TimeSpan.FromSeconds(1));

        // Assert
        Assert.Equal(1, backend.RunCount);

        backend.Finish();
        await active;
    }

    [Fact]
    public async Task It_still_finishes_the_run_that_was_already_going()
    {
        // Arrange
        var backend = new HeldTestBackend(CartTest);
        var session = await SessionRunningOneTestAsync(backend);
        var active = session.DispatchAsync(new ExplorerCommand.RunSelected());
        await backend.Started.Task.WaitAsync(TimeSpan.FromSeconds(1));
        await session.DispatchAsync(new ExplorerCommand.RunSelected())
            .WaitAsync(TimeSpan.FromSeconds(1));

        // Act
        backend.Finish();
        await active;

        // Assert
        Assert.Equal(ExplorerStatus.Ready, session.State.Status);
    }

    [Fact]
    public async Task It_accepts_another_run_once_the_first_has_finished()
    {
        // Arrange
        var backend = new HeldTestBackend(CartTest);
        var session = await SessionRunningOneTestAsync(backend);
        backend.Finish();
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
        var session = new TestExplorerSession(backend);
        await session.LoadAsync("/repo/Shop.sln");
        await session.DispatchAsync(new ExplorerCommand.MoveDown());
        await session.DispatchAsync(new ExplorerCommand.MoveDown());
        return session;
    }

    private sealed class HeldTestBackend(TestCase test) : ITestBackend
    {
        private readonly TaskCompletionSource finished =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Started { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int RunCount { get; private set; }

        public void Finish() => finished.TrySetResult();

        public Task<IReadOnlyList<TestCase>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TestCase>>([test]);

        public async Task<TestRun> RunAsync(
            IReadOnlyCollection<TestCase> tests,
            CancellationToken cancellationToken = default)
        {
            RunCount++;
            Started.TrySetResult();
            await finished.Task;
            return new TestRun(true, "Passed");
        }
    }
}
