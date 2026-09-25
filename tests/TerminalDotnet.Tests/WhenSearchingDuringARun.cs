using TerminalDotnet.Explorer;
using TerminalDotnet.Testing;
using TerminalDotnet.Tests.Builders;
using Xunit;

namespace TerminalDotnet.Tests.Testing;

public sealed class WhenSearchingDuringARun
{
    [Fact]
    public async Task It_keeps_showing_the_running_tests_as_running()
    {
        // Arrange
        var backend = new HeldTestBackend(AddsItem);
        var session = await GivenA.TestExplorer()
            .WithBackend(backend)
            .LoadedAsync();
        await session.DispatchAsync(new ExplorerCommand.MoveDown());
        var active = session.DispatchAsync(new ExplorerCommand.RunSelected());
        await backend.Started.Task.WaitAsync(TimeSpan.FromSeconds(1));

        // Act
        await session.DispatchAsync(new ExplorerCommand.Search("Adds"));

        // Assert
        Assert.Equal(TestNodeOutcome.Running, session.State.VisibleNodes[2].Outcome);

        backend.Finish();
        await active;
    }

    private static readonly TestCase AddsItem =
        new("Shop.Tests.CartTests.Adds_item", "Adds item", "Shop.Tests.csproj");

    private sealed class HeldTestBackend(TestCase test) : ITestBackend
    {
        private readonly TaskCompletionSource finished =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Started { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Finish() => finished.TrySetResult();

        public Task<IReadOnlyList<TestCase>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TestCase>>([test]);

        public async Task<TestRun> RunAsync(
            IReadOnlyCollection<TestCase> tests,
            CancellationToken cancellationToken = default)
        {
            Started.TrySetResult();
            await finished.Task;
            return new TestRun(true, "Passed");
        }
    }
}
