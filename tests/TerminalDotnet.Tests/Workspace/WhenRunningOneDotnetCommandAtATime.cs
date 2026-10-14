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
