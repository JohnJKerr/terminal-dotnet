namespace TerminalDotnet.Testing;

/// <summary>
/// One command at a time. A build and a test run both write the same output,
/// and a second one started while the first still holds those files fails or
/// waits on them for the whole build.
/// </summary>
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
