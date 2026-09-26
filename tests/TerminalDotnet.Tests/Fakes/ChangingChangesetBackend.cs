using TerminalDotnet.Changes;

namespace TerminalDotnet.Tests.Fakes;

/// <summary>A changeset that is empty until the test changes Order.cs.</summary>
internal sealed class ChangingChangesetBackend : IChangesetBackend
{
    public bool Changed { get; set; }

    public Task<IReadOnlyList<ChangedFile>> DiscoverAsync(
        string target,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ChangedFile>>(Changed
            ? [new ChangedFile("/repo/Order.cs", "Order.cs", ChangeKind.Modified)]
            : []);

    public Task<string> DiffAsync(ChangedFile file, CancellationToken cancellationToken = default) =>
        Task.FromResult("");

    public Task<bool> RestoreAsync(ChangedFile file, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);
}
