using TerminalDotnet.Files;
using TerminalDotnet.Filters;
using Xunit;

namespace TerminalDotnet.Tests.Explorer;

public sealed class WhenFilteringToUpdatedFiles
{
    [Fact]
    public async Task It_keeps_only_the_new_and_changed_files()
    {
        // Arrange
        var session = await LoadedSessionAsync();

        // Act
        await session.DispatchAsync(new FileExplorerCommand.ToggleFilter(ExplorerFilter.Updated));

        // Assert
        Assert.Equal(
            ["Added.cs", "Changed.cs"],
            FileNames(session.State));
    }

    [Fact]
    public async Task It_remembers_the_filter_it_is_using()
    {
        // Arrange
        var session = await LoadedSessionAsync();

        // Act
        await session.DispatchAsync(new FileExplorerCommand.ToggleFilter(ExplorerFilter.Updated));

        // Assert
        Assert.Equal(ExplorerFilter.Updated, session.State.ActiveFilter);
    }

    [Fact]
    public async Task It_brings_every_file_back_when_the_same_filter_is_pressed_again()
    {
        // Arrange
        var session = await LoadedSessionAsync();
        await session.DispatchAsync(new FileExplorerCommand.ToggleFilter(ExplorerFilter.Updated));

        // Act
        await session.DispatchAsync(new FileExplorerCommand.ToggleFilter(ExplorerFilter.Updated));

        // Assert
        Assert.Equal(["Added.cs", "Changed.cs", "Order.cs"], FileNames(session.State));
    }

    [Fact]
    public async Task It_forgets_the_filter_when_the_same_filter_is_pressed_again()
    {
        // Arrange
        var session = await LoadedSessionAsync();
        await session.DispatchAsync(new FileExplorerCommand.ToggleFilter(ExplorerFilter.Updated));

        // Act
        await session.DispatchAsync(new FileExplorerCommand.ToggleFilter(ExplorerFilter.Updated));

        // Assert
        Assert.Null(session.State.ActiveFilter);
    }

    [Fact]
    public async Task It_narrows_the_updated_files_to_the_search()
    {
        // Arrange
        var session = await LoadedSessionAsync();
        await session.DispatchAsync(new FileExplorerCommand.ToggleFilter(ExplorerFilter.Updated));

        // Act
        await session.DispatchAsync(new FileExplorerCommand.Search("changed"));

        // Assert
        Assert.Equal(["Changed.cs"], FileNames(session.State));
    }

    [Fact]
    public async Task It_keeps_the_filter_while_the_search_is_cleared()
    {
        // Arrange
        var session = await LoadedSessionAsync();
        await session.DispatchAsync(new FileExplorerCommand.ToggleFilter(ExplorerFilter.Updated));
        await session.DispatchAsync(new FileExplorerCommand.Search("changed"));

        // Act
        await session.DispatchAsync(new FileExplorerCommand.ClearSearch());

        // Assert
        Assert.Equal(["Added.cs", "Changed.cs"], FileNames(session.State));
    }

    [Fact]
    public async Task It_selects_the_first_row_of_the_filtered_tree()
    {
        // Arrange
        var session = await LoadedSessionAsync();
        await session.DispatchAsync(new FileExplorerCommand.MoveDown());
        await session.DispatchAsync(new FileExplorerCommand.MoveDown());

        // Act
        await session.DispatchAsync(new FileExplorerCommand.ToggleFilter(ExplorerFilter.Updated));

        // Assert
        Assert.Equal(0, session.State.SelectedIndex);
    }

    private static async Task<FileExplorerSession> LoadedSessionAsync()
    {
        var session = new FileExplorerSession(new InMemoryFileExplorerBackend(
        [
            new FileEntry("src/App/App.csproj", "src/App/Order.cs", FileGitStatus.Unchanged),
            new FileEntry("src/App/App.csproj", "src/App/Added.cs", FileGitStatus.New),
            new FileEntry("src/App/App.csproj", "src/App/Changed.cs", FileGitStatus.Modified),
            new FileEntry("src/App/App.csproj", "src/App/Gone.cs", FileGitStatus.Deleted)
        ]));
        await session.LoadAsync("TerminalDotnet.slnx");
        return session;
    }

    private static IEnumerable<string> FileNames(FileExplorerState state) => state.VisibleNodes
        .Where(node => node.Kind == FileNodeKind.File)
        .Select(node => node.Name);

    private sealed class InMemoryFileExplorerBackend(IReadOnlyList<FileEntry> entries) : IFileExplorerBackend
    {
        public Task<IReadOnlyList<FileEntry>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default) => Task.FromResult(entries);
    }
}
