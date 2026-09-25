using TerminalDotnet.Files;

namespace TerminalDotnet.Tests.Fakes;

/// <summary>A tree of one file, which the test can change between reads.
/// </summary>
internal sealed class ChangingFileBackend(FileEntry file) : IFileExplorerBackend
{
    public FileEntry File { get; set; } = file;

    public Task<IReadOnlyList<FileEntry>> DiscoverAsync(
        string target,
        CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<FileEntry>>([File]);
}
