using TerminalDotnet.Files;

namespace TerminalDotnet.Tests.Fakes;

/// <summary>A tree that holds the same files every time it is read.</summary>
internal sealed class FixedFiles(params FileEntry[] entries) : IFileExplorerBackend
{
    public Task<IReadOnlyList<FileEntry>> DiscoverAsync(
        string target,
        CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<FileEntry>>(entries);
}
