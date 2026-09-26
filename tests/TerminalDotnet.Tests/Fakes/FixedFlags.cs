using TerminalDotnet.Flags;

namespace TerminalDotnet.Tests.Fakes;

/// <summary>A tree that flags the same lines every time it is read.</summary>
internal sealed class FixedFlags(params Flag[] flags) : IFlagBackend
{
    public Task<IReadOnlyList<Flag>> DiscoverAsync(string target, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Flag>>(flags);
}
