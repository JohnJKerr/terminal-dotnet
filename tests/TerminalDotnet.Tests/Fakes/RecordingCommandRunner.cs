using TerminalDotnet.Testing;

namespace TerminalDotnet.Tests.Fakes;

/// <summary>Answers every command with the same result and keeps the last
/// command it was asked to run.</summary>
internal sealed class RecordingCommandRunner(CommandResult? result = null) : ICommandRunner
{
    public CommandRequest? LastRequest { get; private set; }

    public Task<CommandResult> RunAsync(CommandRequest request, CancellationToken cancellationToken = default)
    {
        LastRequest = request;
        return Task.FromResult(result ?? new CommandResult(0, "", ""));
    }
}
