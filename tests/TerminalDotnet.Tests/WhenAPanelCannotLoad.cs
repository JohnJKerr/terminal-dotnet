using TerminalDotnet.Changes;
using TerminalDotnet.Files;
using Xunit;

namespace TerminalDotnet.Tests.Explorer;

public sealed class WhenAPanelCannotLoad
{
    [Fact]
    public async Task The_explorer_stops_waiting_for_files_that_will_never_arrive()
    {
        // Arrange
        var session = new FileExplorerSession(new FailingFileBackend());

        // Act
        await session.LoadAsync("App.csproj");

        // Assert
        Assert.False(session.State.Loading);
    }

    [Fact]
    public async Task The_explorer_keeps_an_empty_tree()
    {
        // Arrange
        var session = new FileExplorerSession(new FailingFileBackend());

        // Act
        await session.LoadAsync("App.csproj");

        // Assert
        Assert.Empty(session.State.VisibleNodes);
    }

    [Fact]
    public async Task The_changeset_stops_waiting_for_changes_that_will_never_arrive()
    {
        // Arrange
        var session = new ChangesetSession(new FailingChangesetBackend());

        // Act
        await session.LoadAsync("App.csproj");

        // Assert
        Assert.False(session.State.Loading);
    }

    [Fact]
    public async Task The_changeset_keeps_an_empty_list()
    {
        // Arrange
        var session = new ChangesetSession(new FailingChangesetBackend());

        // Act
        await session.LoadAsync("App.csproj");

        // Assert
        Assert.Empty(session.State.Files);
    }

    private sealed class FailingFileBackend : IFileExplorerBackend
    {
        public Task<IReadOnlyList<FileEntry>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<FileEntry>>(
                new InvalidOperationException("Could not start git."));
    }

    private sealed class FailingChangesetBackend : IChangesetBackend
    {
        public Task<IReadOnlyList<ChangedFile>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<ChangedFile>>(
                new InvalidOperationException("Could not start git."));

        public Task<string> DiffAsync(ChangedFile file, CancellationToken cancellationToken = default) =>
            Task.FromResult("");

        public Task<bool> RestoreAsync(ChangedFile file, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }
}
