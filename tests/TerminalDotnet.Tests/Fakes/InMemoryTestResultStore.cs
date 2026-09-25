using TerminalDotnet.Testing;

namespace TerminalDotnet.Tests.Fakes;

/// <summary>A results file that always holds the same contents, and
/// remembers each path it was told to discard.</summary>
internal sealed class InMemoryTestResultStore(string contents = "<TestRun />") : ITestResultStore
{
    public const string ResultPath = "/tmp/terminal-dotnet.trx";

    public List<string> Discarded { get; } = [];

    public string CreatePath() => ResultPath;

    public Task<string> ReadAsync(string path, CancellationToken cancellationToken = default) =>
        Task.FromResult(contents);

    public void Discard(string path) => Discarded.Add(path);
}
