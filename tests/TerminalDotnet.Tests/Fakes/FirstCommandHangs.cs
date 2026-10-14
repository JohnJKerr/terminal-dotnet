using TerminalDotnet.Testing;

namespace TerminalDotnet.Tests.Fakes;

/// <summary>A command runner whose first command runs until it is cancelled,
/// and whose later commands finish at once.</summary>
internal sealed class FirstCommandHangs : ICommandRunner
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
