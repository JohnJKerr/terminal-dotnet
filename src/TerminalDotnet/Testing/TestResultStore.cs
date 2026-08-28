namespace TerminalDotnet.Testing;

public interface ITestResultStore
{
    string CreatePath();

    Task<string> ReadAsync(string path, CancellationToken cancellationToken = default);
}

public sealed class TemporaryTrxResultStore : ITestResultStore
{
    public string CreatePath() =>
        Path.Combine(Path.GetTempPath(), $"terminal-dotnet-{Guid.NewGuid():N}.trx");

    public async Task<string> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        var contents = await File.ReadAllTextAsync(path, cancellationToken);
        File.Delete(path);
        return contents;
    }
}
