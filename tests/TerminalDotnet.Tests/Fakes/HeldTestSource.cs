using TerminalDotnet.Explorer;
using TerminalDotnet.Testing;

namespace TerminalDotnet.Tests.Fakes;

internal sealed class HeldTestSource : ITestSourceLocator
{
    public TaskCompletionSource<SourceLocation?> First { get; } = new();
    private int calls;

    public Task<SourceLocation?> LocateAsync(TestCase test, CancellationToken cancellationToken = default) =>
        Interlocked.Increment(ref calls) == 1
            ? First.Task
            : Task.FromResult<SourceLocation?>(new SourceLocation("latest.cs", 2));
}
