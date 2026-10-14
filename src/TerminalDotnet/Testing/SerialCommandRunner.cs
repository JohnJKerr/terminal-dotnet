namespace TerminalDotnet.Testing;

public sealed class SerialCommandRunner(ICommandRunner inner) : ICommandRunner
{
    private readonly SemaphoreSlim gate = new(1, 1);

    public async Task<CommandResult> RunAsync(
        CommandRequest request,
        CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await inner.RunAsync(request, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
        }
    }
}
