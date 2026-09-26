using TerminalDotnet.Changes;
using TerminalDotnet.Comments;
using TerminalDotnet.Files;
using TerminalDotnet.Filters;
using TerminalDotnet.Flags;
using TerminalDotnet.Issues;
using TerminalDotnet.Tests.Builders;
using Xunit;

namespace TerminalDotnet.Tests.Workspace;

/// <summary>An agent editing alongside the reader reloads the panels without
/// being asked, so a reload must not take the reader's place away from them.
/// </summary>
public sealed class WhenThePanelsReloadUnderTheReader
{
    [Fact]
    public async Task The_file_explorer_keeps_the_search_the_reader_typed()
    {
        // Arrange
        var explorer = new FileExplorerSession(Files("Order.cs", "Basket.cs"));
        await explorer.LoadAsync("App.csproj");
        await explorer.DispatchAsync(new FileExplorerCommand.Search("Order"));

        // Act
        await explorer.LoadAsync("App.csproj");

        // Assert
        Assert.Equal("Order", explorer.State.SearchQuery);
    }

    [Fact]
    public async Task The_file_explorer_shows_only_what_that_search_matches()
    {
        // Arrange
        var explorer = new FileExplorerSession(Files("Order.cs", "Basket.cs"));
        await explorer.LoadAsync("App.csproj");
        await explorer.DispatchAsync(new FileExplorerCommand.Search("Order"));

        // Act
        await explorer.LoadAsync("App.csproj");

        // Assert
        Assert.Equal(1, explorer.State.VisibleFileCount);
    }

    [Fact]
    public async Task The_file_explorer_keeps_the_filter_the_reader_turned_on()
    {
        // Arrange
        var explorer = new FileExplorerSession(Files("Order.cs", "Basket.cs"));
        await explorer.LoadAsync("App.csproj");
        await explorer.DispatchAsync(new FileExplorerCommand.ToggleFilter(ExplorerFilter.Updated));

        // Act
        await explorer.LoadAsync("App.csproj");

        // Assert
        Assert.Equal(ExplorerFilter.Updated, explorer.State.ActiveFilter);
    }

    [Fact]
    public async Task The_file_explorer_keeps_the_row_the_reader_was_on()
    {
        // Arrange
        var backend = Files("Order.cs", "Basket.cs");
        var explorer = new FileExplorerSession(backend);
        await explorer.LoadAsync("App.csproj");
        var wasOn = explorer.State.VisibleNodes[^1].Name;
        await explorer.DispatchAsync(
            new FileExplorerCommand.SelectIndex(explorer.State.VisibleNodes.Count - 1));
        backend.Add("Alpha.cs");

        // Act
        await explorer.LoadAsync("App.csproj");

        // Assert
        Assert.Equal(wasOn, explorer.State.VisibleNodes[explorer.State.SelectedIndex].Name);
    }

    [Fact]
    public async Task The_file_explorer_settles_on_the_last_row_when_the_one_it_was_on_went_away()
    {
        // Arrange
        var backend = Files("Order.cs", "Basket.cs");
        var explorer = new FileExplorerSession(backend);
        await explorer.LoadAsync("App.csproj");
        await explorer.DispatchAsync(
            new FileExplorerCommand.SelectIndex(explorer.State.VisibleNodes.Count - 1));
        backend.Remove("Order.cs");
        backend.Remove("Basket.cs");

        // Act
        await explorer.LoadAsync("App.csproj");

        // Assert
        Assert.Equal(0, explorer.State.SelectedIndex);
    }

    [Fact]
    public async Task The_changeset_keeps_the_row_the_reader_was_on()
    {
        // Arrange
        var backend = new GrowingChangesetBackend("Order.cs", "Basket.cs");
        var changes = new ChangesetSession(backend);
        await changes.LoadAsync("App.csproj");
        await changes.DispatchAsync(new ChangesetCommand.SelectIndex(1));
        backend.Add("Alpha.cs");

        // Act
        await changes.LoadAsync("App.csproj");

        // Assert
        Assert.Equal("Basket.cs", changes.State.Files[changes.State.SelectedIndex].DisplayPath);
    }

    [Fact]
    public async Task The_flags_keep_the_row_the_reader_was_on()
    {
        // Arrange
        var backend = new GrowingFlagBackend("Order.cs", "Basket.cs");
        var issues = GivenA.IssuePanel().WithFlagBackend(backend).Build();
        await issues.LoadFlagsAsync("App.csproj");
        await issues.DispatchAsync(new IssueCommand.SelectIndex(1));
        backend.Add("Alpha.cs");

        // Act
        await issues.LoadFlagsAsync("App.csproj");

        // Assert
        Assert.Equal("Order.cs", issues.State.Issues[issues.State.SelectedIndex].DisplayPath);
    }

    private static GrowingFileBackend Files(params string[] names) => new(names);

    private sealed class GrowingFileBackend(IEnumerable<string> names) : IFileExplorerBackend
    {
        private readonly List<string> files = [.. names];

        public void Add(string name) => files.Insert(0, name);

        public void Remove(string name) => files.Remove(name);

        public Task<IReadOnlyList<FileEntry>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FileEntry>>(
                [.. files.Select(name => new FileEntry("App.csproj", name, FileGitStatus.Modified))]);
    }

    private sealed class GrowingChangesetBackend(params string[] names) : IChangesetBackend
    {
        private readonly List<string> files = [.. names];

        public void Add(string name) => files.Insert(0, name);

        public Task<IReadOnlyList<ChangedFile>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ChangedFile>>(
                [.. files.Select(name => new ChangedFile($"/repo/{name}", name, ChangeKind.Modified))]);

        public Task<string> DiffAsync(ChangedFile file, CancellationToken cancellationToken = default) =>
            Task.FromResult("");

        public Task<bool> RestoreAsync(ChangedFile file, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    private sealed class GrowingFlagBackend(params string[] names) : IFlagBackend
    {
        private readonly List<string> files = [.. names];

        public void Add(string name) => files.Insert(0, name);

        public Task<IReadOnlyList<Flag>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Flag>>(
                [.. files.Select(name => new Flag($"/repo/{name}", name, 1, FlagKind.Todo, "later"))]);
    }
}
