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

    private sealed class InMemoryFileExplorerBackend(IReadOnlyList<FileEntry> entries) : IFileExplorerBackend
    {
        public Task<IReadOnlyList<FileEntry>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default) => Task.FromResult(entries);
    }
}
