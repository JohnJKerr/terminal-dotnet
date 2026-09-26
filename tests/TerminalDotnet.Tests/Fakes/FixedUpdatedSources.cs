using TerminalDotnet.Explorer;

namespace TerminalDotnet.Tests.Fakes;

/// <summary>Reports the same updated sources whatever it is asked about.
/// </summary>
internal sealed class FixedUpdatedSources(params UpdatedSource[] sources) : IUpdatedSourceProvider
{
    public Task<IReadOnlyList<UpdatedSource>> UpdatedSourcesAsync(
        string target,
        CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<UpdatedSource>>(sources);
}
