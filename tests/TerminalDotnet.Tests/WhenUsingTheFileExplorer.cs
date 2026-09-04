using TerminalDotnet.Files;
using Xunit;

namespace TerminalDotnet.Tests.Explorer;

public sealed class WhenUsingTheFileExplorer
{
    [Fact]
    public async Task It_shows_projects_namespaces_and_files()
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
    public async Task It_hides_the_selected_projects_descendants_when_collapsed()
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
    public async Task It_keeps_the_selection_on_the_node_it_folded()
    {
        // Arrange
        var session = SessionWithFiles(
            new FileEntry("src/App/App.csproj", "App.Domain", "src/App/Order.cs", FileGitStatus.Unchanged));
        await session.LoadAsync("TerminalDotnet.slnx");
        await session.DispatchAsync(new FileExplorerCommand.MoveDown());

        // Act
        await session.DispatchAsync(new FileExplorerCommand.ToggleExpanded());

        // Assert
        Assert.Equal("App.Domain", session.State.VisibleNodes[session.State.SelectedIndex].Name);
    }

    [Fact]
    public async Task It_selects_the_next_visible_node_when_moving_down()
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
    public async Task It_shows_fuzzy_file_matches_with_their_ancestors()
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
    public async Task It_restores_all_files_when_search_is_cleared()
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
    public async Task It_selects_matching_files_and_wraps_when_moving_forward()
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

    [Fact]
    public async Task It_wraps_to_the_last_matching_file_when_moving_backward()
    {
        // Arrange
        var session = SessionWithFiles(
            new FileEntry("src/App/App.csproj", "App.Domain", "src/App/Order.cs", FileGitStatus.Unchanged),
            new FileEntry("src/App/App.csproj", "App.Domain", "src/App/OrderHandler.cs", FileGitStatus.Unchanged));
        await session.LoadAsync("TerminalDotnet.slnx");
        await session.DispatchAsync(new FileExplorerCommand.Search("order"));

        // Act
        await session.DispatchAsync(new FileExplorerCommand.PreviousSearchMatch());

        // Assert
        Assert.Equal("OrderHandler.cs", session.State.VisibleNodes[session.State.SelectedIndex].Name);
    }

    [Fact]
    public async Task It_counts_the_files_it_discovered_and_the_changes_among_them()
    {
        // Arrange
        var session = SessionWithFiles(
            new FileEntry("src/App/App.csproj", "App", "src/App/Order.cs", FileGitStatus.Unchanged),
            new FileEntry("src/App/App.csproj", "App", "src/App/Added.cs", FileGitStatus.New),
            new FileEntry("src/App/App.csproj", "App", "src/App/Changed.cs", FileGitStatus.Modified),
            new FileEntry("src/App/App.csproj", "", "src/App/Gone.cs", FileGitStatus.Deleted));

        // Act
        await session.LoadAsync("TerminalDotnet.slnx");

        // Assert
        Assert.Equal(new FileChangeSummary(3, 1, 1, 1), session.State.Changes);
    }

    [Fact]
    public async Task It_leaves_deleted_files_out_of_the_tree()
    {
        // Arrange
        var session = SessionWithFiles(
            new FileEntry("src/App/App.csproj", "App", "src/App/Order.cs", FileGitStatus.Unchanged),
            new FileEntry("src/App/App.csproj", "", "src/App/Gone.cs", FileGitStatus.Deleted));

        // Act
        await session.LoadAsync("TerminalDotnet.slnx");

        // Assert
        Assert.DoesNotContain("Gone.cs", session.State.VisibleNodes.Select(node => node.Name));
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
