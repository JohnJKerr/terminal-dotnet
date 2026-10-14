namespace TerminalDotnet.Testing;

public sealed class SerialCommandRunner(ICommandRunner inner, TimeSpan? commandLimit = null) : ICommandRunner
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly TimeSpan limit = commandLimit ?? TimeSpan.FromHours(1);

    public async Task<CommandResult> RunAsync(
        CommandRequest request,
        CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await RunWithinLimitAsync(request, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<CommandResult> RunWithinLimitAsync(
        CommandRequest request,
        CancellationToken cancellationToken)
    {
        using var limited = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limited.CancelAfter(limit);
        try
        {
            return await inner.RunAsync(request, limited.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"{request.FileName} {string.Join(' ', request.Arguments.Take(2))} did not finish within {limit}.");
        }
    }
}
