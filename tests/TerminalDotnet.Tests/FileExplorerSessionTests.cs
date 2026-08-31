using TerminalDotnet.Files;
using Xunit;

namespace TerminalDotnet.Tests;

public sealed class FileExplorerSessionTests
{
    [Fact]
    public async Task LoadShowsProjectsNamespacesAndFiles()
    {
        // Arrange
        var session = new FileExplorerSession(new InMemoryFileExplorerBackend(
        [
            new FileEntry("src/App/App.csproj", "App.Domain", "src/App/Order.cs", FileGitStatus.Unchanged)
        ]));

        // Act
        await session.LoadAsync("TerminalDotnet.slnx");

        // Assert
        Assert.Equal(
        [
            (0, FileNodeKind.Project, "App"),
            (1, FileNodeKind.Namespace, "App.Domain"),
            (2, FileNodeKind.File, "Order.cs")
        ],
        session.State.VisibleNodes.Select(node => (node.Depth, node.Kind, node.Name)));
    }

    [Fact]
    public async Task ToggleExpandedHidesSelectedProjectsDescendants()
    {
        // Arrange
        var session = SessionWithFiles(
            new FileEntry("src/App/App.csproj", "App.Domain", "src/App/Order.cs", FileGitStatus.Unchanged));
        await session.LoadAsync("TerminalDotnet.slnx");

        // Act
        await session.DispatchAsync(new FileExplorerCommand.ToggleExpanded());

        // Assert
        Assert.Equal(["App"], session.State.VisibleNodes.Select(node => node.Name));
    }

    private static FileExplorerSession SessionWithFiles(params FileEntry[] entries) =>
        new(new InMemoryFileExplorerBackend(entries));

    private sealed class InMemoryFileExplorerBackend(IReadOnlyList<FileEntry> entries) : IFileExplorerBackend
    {
        public Task<IReadOnlyList<FileEntry>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default) => Task.FromResult(entries);
    }
}
