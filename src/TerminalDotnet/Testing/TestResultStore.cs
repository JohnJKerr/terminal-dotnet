namespace TerminalDotnet.Testing;

public interface ITestResultStore
{
    string CreatePath();

    Task<string> ReadAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>Removes whatever the run left at the path, whether or not it
    /// finished writing there.</summary>
    void Discard(string path);
}

internal sealed class TemporaryTrxResultStore : ITestResultStore
{
    public string CreatePath() =>
        Path.Combine(Path.GetTempPath(), $"terminal-dotnet-{Guid.NewGuid():N}.trx");

    public Task<string> ReadAsync(string path, CancellationToken cancellationToken = default) =>
        File.ReadAllTextAsync(path, cancellationToken);

    public void Discard(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }
}
