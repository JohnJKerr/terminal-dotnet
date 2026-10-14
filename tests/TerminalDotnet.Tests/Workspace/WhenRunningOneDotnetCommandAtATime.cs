using TerminalDotnet.Testing;
using Xunit;

namespace TerminalDotnet.Tests.Workspace;

public sealed class WhenRunningOneDotnetCommandAtATime
{
    [Fact]
    public async Task The_second_command_waits_until_the_first_has_finished()
    {
        // Arrange
        var inner = new HoldingRunner();
        var runner = new SerialCommandRunner(inner);
        var first = runner.RunAsync(Command("first"));
        await inner.Entered.Task.WaitAsync(TimeSpan.FromSeconds(1));
        var second = runner.RunAsync(Command("second"));

        // Act
        await Task.Delay(50);
        var runningTogether = inner.EnteredCount;

        inner.Release();
        await Task.WhenAll(first, second);

        // Assert
        Assert.Equal(1, runningTogether);
    }

    [Fact]
    public async Task Cancelling_the_command_that_holds_the_gate_lets_the_next_command_start()
    {
        // Arrange
        var inner = new CancelledWhenHolding();
        var runner = new SerialCommandRunner(inner);
        using var cancellation = new CancellationTokenSource();
        var first = runner.RunAsync(Command("first"), cancellation.Token);
        await inner.Entered.Task.WaitAsync(TimeSpan.FromSeconds(1));
        var second = runner.RunAsync(Command("second"));

        // Act
        await cancellation.CancelAsync();
        await ObserveCancellationAsync(first);
        await second.WaitAsync(TimeSpan.FromSeconds(1));

        // Assert
        Assert.Equal(2, inner.EnteredCount);
    }

    private static async Task ObserveCancellationAsync(Task run)
    {
        try
        {
            await run;
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static CommandRequest Command(string name) => new(name, [], Environment.CurrentDirectory);

    private sealed class HoldingRunner : ICommandRunner
    {
        private readonly TaskCompletionSource hold = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int EnteredCount { get; private set; }

        public void Release() => hold.TrySetResult();

        public async Task<CommandResult> RunAsync(CommandRequest request, CancellationToken cancellationToken = default)
        {
            EnteredCount++;
            Entered.TrySetResult();
            await hold.Task;
            return new CommandResult(0, "", "");
        }
    }

    private sealed class CancelledWhenHolding : ICommandRunner
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int EnteredCount { get; private set; }

        public async Task<CommandResult> RunAsync(CommandRequest request, CancellationToken cancellationToken = default)
        {
            var holding = EnteredCount == 0;
            EnteredCount++;
            Entered.TrySetResult();
            if (holding)
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }

            return new CommandResult(0, "", "");
        }
    }
}
