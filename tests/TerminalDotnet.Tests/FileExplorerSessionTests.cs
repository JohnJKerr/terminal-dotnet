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

    [Fact]
    public async Task MoveDownSelectsTheNextVisibleNode()
    {
        // Arrange
        var session = SessionWithFiles(
            new FileEntry("src/App/App.csproj", "App.Domain", "src/App/Order.cs", FileGitStatus.Unchanged));
        await session.LoadAsync("TerminalDotnet.slnx");

        // Act
        await session.DispatchAsync(new FileExplorerCommand.MoveDown());

        // Assert
        Assert.Equal("App.Domain", session.State.VisibleNodes[session.State.SelectedIndex].Name);
    }

    [Fact]
    public async Task SearchShowsFuzzyFileMatchesWithTheirAncestors()
    {
        // Arrange
        var session = SessionWithFiles(
            new FileEntry("src/App/App.csproj", "App.Domain", "src/App/OrderRepository.cs", FileGitStatus.Unchanged),
            new FileEntry("src/App/App.csproj", "App.Domain", "src/App/Customer.cs", FileGitStatus.Unchanged));
        await session.LoadAsync("TerminalDotnet.slnx");

        // Act
        await session.DispatchAsync(new FileExplorerCommand.Search("odr"));

        // Assert
        Assert.Equal(
            ["App", "App.Domain", "OrderRepository.cs"],
            session.State.VisibleNodes.Select(node => node.Name));
    }

    [Fact]
    public async Task ClearSearchRestoresAllFiles()
    {
        // Arrange
        var session = SessionWithFiles(
            new FileEntry("src/App/App.csproj", "App.Domain", "src/App/Order.cs", FileGitStatus.Unchanged),
            new FileEntry("src/App/App.csproj", "App.Domain", "src/App/Customer.cs", FileGitStatus.Unchanged));
        await session.LoadAsync("TerminalDotnet.slnx");
        await session.DispatchAsync(new FileExplorerCommand.Search("order"));

        // Act
        await session.DispatchAsync(new FileExplorerCommand.ClearSearch());

        // Assert
        Assert.Equal(
            ("", 2),
            (session.State.SearchQuery, session.State.VisibleNodes.Count(node => node.Kind == FileNodeKind.File)));
    }

    [Fact]
    public async Task NextSearchMatchSelectsMatchingFilesAndWraps()
    {
        // Arrange
        var session = SessionWithFiles(
            new FileEntry("src/App/App.csproj", "App.Domain", "src/App/Order.cs", FileGitStatus.Unchanged),
            new FileEntry("src/App/App.csproj", "App.Domain", "src/App/OrderHandler.cs", FileGitStatus.Unchanged));
        await session.LoadAsync("TerminalDotnet.slnx");
        await session.DispatchAsync(new FileExplorerCommand.Search("order"));

        // Act
        await session.DispatchAsync(new FileExplorerCommand.NextSearchMatch());
        await session.DispatchAsync(new FileExplorerCommand.NextSearchMatch());
        await session.DispatchAsync(new FileExplorerCommand.NextSearchMatch());

        // Assert
        Assert.Equal("Order.cs", session.State.VisibleNodes[session.State.SelectedIndex].Name);
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
