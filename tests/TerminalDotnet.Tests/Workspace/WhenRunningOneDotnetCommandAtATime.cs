using TerminalDotnet.Testing;
using TerminalDotnet.Tests.Fakes;
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
        var inner = new FirstCommandHangs();
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

    [Fact]
    public async Task A_command_that_runs_past_the_limit_lets_the_next_command_start()
    {
        // Arrange
        var inner = new FirstCommandHangs();
        var runner = new SerialCommandRunner(inner, TimeSpan.FromMilliseconds(50));
        var first = runner.RunAsync(Command("first"));
        await inner.Entered.Task.WaitAsync(TimeSpan.FromSeconds(1));

        // Act
        var second = await runner.RunAsync(Command("second")).WaitAsync(TimeSpan.FromSeconds(2));
        await ObserveTimeoutAsync(first);

        // Assert
        Assert.Equal(0, second.ExitCode);
    }

    [Fact]
    public async Task A_command_that_runs_past_the_limit_is_reported_as_unfinished()
    {
        // Arrange
        var runner = new SerialCommandRunner(new FirstCommandHangs(), TimeSpan.FromMilliseconds(50));

        // Act
        var run = runner.RunAsync(Command("dotnet"));

        // Assert
        await Assert.ThrowsAsync<TimeoutException>(() => run.WaitAsync(TimeSpan.FromSeconds(2)));
    }

    private static async Task ObserveTimeoutAsync(Task run)
    {
        try
        {
            await run;
        }
        catch (TimeoutException)
        {
        }
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
}
