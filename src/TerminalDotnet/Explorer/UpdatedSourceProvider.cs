using TerminalDotnet.Changes;

namespace TerminalDotnet.Explorer;

public sealed record UpdatedSource(string Path, ChangeKind Change);

public interface IUpdatedSourceProvider
{
    Task<IReadOnlyList<UpdatedSource>> UpdatedSourcesAsync(
        string target,
        CancellationToken cancellationToken = default);
}

public sealed class ChangesetUpdatedSourceProvider(IChangesetBackend backend) : IUpdatedSourceProvider
{
    public async Task<IReadOnlyList<UpdatedSource>> UpdatedSourcesAsync(
        string target,
        CancellationToken cancellationToken = default)
    {
        var changes = await backend.DiscoverAsync(target, cancellationToken);
        return changes
            .Where(file => file.Kind != ChangeKind.Deleted)
            .Select(file => new UpdatedSource(file.Path, file.Kind))
            .ToArray();
    }
}
