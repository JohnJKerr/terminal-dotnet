using TerminalDotnet.Changes;

namespace TerminalDotnet.Tests.Fakes;

/// <summary>A changeset held in memory. Restoring a file takes it out of the
/// changeset, unless the backend is told to refuse.</summary>
internal sealed class InMemoryChangesetBackend(params ChangedFile[] files) : IChangesetBackend
{
    private readonly List<ChangedFile> changedFiles = [.. files];

    public List<ChangedFile> Restored { get; } = [];

    public bool RestoreSucceeds { get; init; } = true;

    public Task<IReadOnlyList<ChangedFile>> DiscoverAsync(
        string target,
        CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ChangedFile>>([.. changedFiles]);

    public Task<string> DiffAsync(ChangedFile file, CancellationToken cancellationToken = default) =>
        Task.FromResult($"diff for {file.DisplayPath}");

    public Task<bool> RestoreAsync(ChangedFile file, CancellationToken cancellationToken = default)
    {
        if (!RestoreSucceeds)
        {
            return Task.FromResult(false);
        }

        Restored.Add(file);
        changedFiles.Remove(file);
        return Task.FromResult(true);
    }
}
