using TerminalDotnet.Files;
using Xunit;

namespace TerminalDotnet.Tests.Explorer;

public sealed class WhenUsingTheFileExplorer
{
    [Fact]
    public async Task It_nests_a_node_for_every_folder_between_the_project_and_the_file()
    {
        // Arrange
        var session = SessionWithFiles(new FileEntry(
            "src/App/App.csproj",
            "src/App/Controllers/Api/OrdersController.cs",
            FileGitStatus.Unchanged));

        // Act
        await session.LoadAsync("TerminalDotnet.slnx");

        // Assert
        Assert.Equal(
        [
            (0, FileNodeKind.Project, "App"),
            (1, FileNodeKind.Folder, "Controllers"),
            (2, FileNodeKind.Folder, "Api"),
            (3, FileNodeKind.File, "OrdersController.cs")
        ],
        session.State.VisibleNodes.Select(node => (node.Depth, node.Kind, node.Name)));
    }

    [Fact]
    public async Task It_hangs_files_in_the_project_root_directly_off_the_project()
    {
        // Arrange
        var session = SessionWithFiles(
            new FileEntry("src/App/App.csproj", "src/App/appsettings.json", FileGitStatus.Unchanged));

        // Act
        await session.LoadAsync("TerminalDotnet.slnx");

        // Assert
        Assert.Equal(
            [(0, FileNodeKind.Project, "App"), (1, FileNodeKind.File, "appsettings.json")],
            session.State.VisibleNodes.Select(node => (node.Depth, node.Kind, node.Name)));
    }

    [Fact]
    public async Task It_lists_folders_before_the_files_beside_them()
    {
        // Arrange
        var session = SessionWithFiles(
            new FileEntry("src/App/App.csproj", "src/App/appsettings.json", FileGitStatus.Unchanged),
            new FileEntry("src/App/App.csproj", "src/App/Models/Order.cs", FileGitStatus.Unchanged));

        // Act
        await session.LoadAsync("TerminalDotnet.slnx");

        // Assert
        Assert.Equal(
            ["App", "Models", "Order.cs", "appsettings.json"],
            session.State.VisibleNodes.Select(node => node.Name));
    }

    [Fact]
    public async Task It_hides_a_whole_subtree_when_its_folder_is_collapsed()
    {
        // Arrange
        var session = SessionWithFiles(new FileEntry(
            "src/App/App.csproj",
            "src/App/Controllers/Api/OrdersController.cs",
            FileGitStatus.Unchanged));
        await session.LoadAsync("TerminalDotnet.slnx");
        await session.DispatchAsync(new FileExplorerCommand.MoveDown());

        // Act
        await session.DispatchAsync(new FileExplorerCommand.ToggleExpanded());

        // Assert
        Assert.Equal(["App", "Controllers"], session.State.VisibleNodes.Select(node => node.Name));
    }

    [Fact]
    public async Task It_folds_the_folder_it_was_told_to_and_not_its_namesake_elsewhere()
    {
        // Arrange
        var session = SessionWithFiles(
            new FileEntry("src/App/App.csproj", "src/App/Orders/Shared/Order.cs", FileGitStatus.Unchanged),
            new FileEntry("src/App/App.csproj", "src/App/Carts/Shared/Cart.cs", FileGitStatus.Unchanged));
        await session.LoadAsync("TerminalDotnet.slnx");
        await session.DispatchAsync(new FileExplorerCommand.MoveDown());
        await session.DispatchAsync(new FileExplorerCommand.MoveDown());

        // Act
        await session.DispatchAsync(new FileExplorerCommand.ToggleExpanded());

        // Assert
        Assert.Equal(
            ["App", "Carts", "Shared", "Orders", "Shared", "Order.cs"],
            session.State.VisibleNodes.Select(node => node.Name));
    }

    [Fact]
    public async Task It_hides_the_selected_projects_descendants_when_collapsed()
    {
        // Arrange
        var session = SessionWithFiles(
            new FileEntry("src/App/App.csproj", "src/App/Order.cs", FileGitStatus.Unchanged));
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
            new FileEntry("src/App/App.csproj", "src/App/Models/Order.cs", FileGitStatus.Unchanged));
        await session.LoadAsync("TerminalDotnet.slnx");
        await session.DispatchAsync(new FileExplorerCommand.MoveDown());

        // Act
        await session.DispatchAsync(new FileExplorerCommand.ToggleExpanded());

        // Assert
        Assert.Equal("Models", session.State.VisibleNodes[session.State.SelectedIndex].Name);
    }

    [Fact]
    public async Task It_selects_the_next_visible_node_when_moving_down()
    {
        // Arrange
        var session = SessionWithFiles(
            new FileEntry("src/App/App.csproj", "src/App/Models/Order.cs", FileGitStatus.Unchanged));
        await session.LoadAsync("TerminalDotnet.slnx");

        // Act
        await session.DispatchAsync(new FileExplorerCommand.MoveDown());

        // Assert
        Assert.Equal("Models", session.State.VisibleNodes[session.State.SelectedIndex].Name);
    }

    [Fact]
    public async Task It_shows_matching_files_with_their_ancestors()
    {
        // Arrange
        var session = SessionWithFiles(
            new FileEntry("src/App/App.csproj", "src/App/OrderRepository.cs", FileGitStatus.Unchanged),
            new FileEntry("src/App/App.csproj", "src/App/Customer.cs", FileGitStatus.Unchanged));
        await session.LoadAsync("TerminalDotnet.slnx");

        // Act
        await session.DispatchAsync(new FileExplorerCommand.Search("orderrep"));

        // Assert
        Assert.Equal(
            ["App", "OrderRepository.cs"],
            session.State.VisibleNodes.Select(node => node.Name));
    }

    [Fact]
    public async Task It_ignores_files_that_only_scatter_the_query_across_their_path()
    {
        // Arrange
        var session = SessionWithFiles(
            new FileEntry("src/App/App.csproj", "src/App/Support/TestingExtensions.cs", FileGitStatus.Unchanged));
        await session.LoadAsync("TerminalDotnet.slnx");

        // Act
        await session.DispatchAsync(new FileExplorerCommand.Search("appsettings"));

        // Assert
        Assert.Empty(session.State.VisibleNodes);
    }

    [Fact]
    public async Task It_restores_all_files_when_search_is_cleared()
    {
        // Arrange
        var session = SessionWithFiles(
            new FileEntry("src/App/App.csproj", "src/App/Order.cs", FileGitStatus.Unchanged),
            new FileEntry("src/App/App.csproj", "src/App/Customer.cs", FileGitStatus.Unchanged));
        await session.LoadAsync("TerminalDotnet.slnx");
        await session.DispatchAsync(new FileExplorerCommand.Search("order"));

        // Act
        await session.DispatchAsync(new FileExplorerCommand.ClearSearch());

        // Assert
        Assert.Equal(2, session.State.VisibleNodes.Count(node => node.Kind == FileNodeKind.File));
    }

    [Fact]
    public async Task It_forgets_the_query_when_search_is_cleared()
    {
        // Arrange
        var session = SessionWithFiles(
            new FileEntry("src/App/App.csproj", "src/App/Order.cs", FileGitStatus.Unchanged));
        await session.LoadAsync("TerminalDotnet.slnx");
        await session.DispatchAsync(new FileExplorerCommand.Search("order"));

        // Act
        await session.DispatchAsync(new FileExplorerCommand.ClearSearch());

        // Assert
        Assert.Equal("", session.State.SearchQuery);
    }

    [Fact]
    public async Task It_selects_matching_files_and_wraps_when_moving_forward()
    {
        // Arrange
        var session = SessionWithFiles(
            new FileEntry("src/App/App.csproj", "src/App/Order.cs", FileGitStatus.Unchanged),
            new FileEntry("src/App/App.csproj", "src/App/OrderHandler.cs", FileGitStatus.Unchanged));
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
            new FileEntry("src/App/App.csproj", "src/App/Order.cs", FileGitStatus.Unchanged),
            new FileEntry("src/App/App.csproj", "src/App/OrderHandler.cs", FileGitStatus.Unchanged));
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
            new FileEntry("src/App/App.csproj", "src/App/Order.cs", FileGitStatus.Unchanged),
            new FileEntry("src/App/App.csproj", "src/App/Added.cs", FileGitStatus.New),
            new FileEntry("src/App/App.csproj", "src/App/Changed.cs", FileGitStatus.Modified),
            new FileEntry("src/App/App.csproj", "src/App/Gone.cs", FileGitStatus.Deleted));

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
            new FileEntry("src/App/App.csproj", "src/App/Order.cs", FileGitStatus.Unchanged),
            new FileEntry("src/App/App.csproj", "src/App/Gone.cs", FileGitStatus.Deleted));

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
