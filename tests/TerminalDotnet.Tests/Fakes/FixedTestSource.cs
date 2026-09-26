using TerminalDotnet.Explorer;
using TerminalDotnet.Testing;

namespace TerminalDotnet.Tests.Fakes;

/// <summary>Finds every test at the same place, or nowhere at all.</summary>
internal sealed class FixedTestSource(SourceLocation? source = null) : ITestSourceLocator
{
    public Task<SourceLocation?> LocateAsync(TestCase test, CancellationToken cancellationToken = default) =>
        Task.FromResult(source);
}
