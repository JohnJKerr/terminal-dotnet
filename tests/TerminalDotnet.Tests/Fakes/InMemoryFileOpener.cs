using TerminalDotnet.Terminal;

namespace TerminalDotnet.Tests.Fakes;

/// <summary>An editor that does whatever the test says a save would do.
/// </summary>
internal sealed class InMemoryFileOpener(Action onOpen) : IFileOpener
{
    public Task OpenAsync(string path, int line, CancellationToken cancellationToken = default)
    {
        onOpen();
        return Task.CompletedTask;
    }
}
