using TerminalDotnet.Changes;
using Xunit;

namespace TerminalDotnet.Tests.Changeset;

public sealed class WhenUsingTheChangeset
{
    [Fact]
    public async Task It_lists_the_added_modified_and_deleted_files()
    {
        // Arrange
        var session = SessionWith(
            new ChangedFile("/repo/src/Added.cs", "src/Added.cs", ChangeKind.Added),
            new ChangedFile("/repo/src/Changed.cs", "src/Changed.cs", ChangeKind.Modified),
            new ChangedFile("/repo/src/Gone.cs", "src/Gone.cs", ChangeKind.Deleted));

        // Act
        await session.LoadAsync("TerminalDotnet.slnx");

        // Assert
        Assert.Equal(
            ["src/Added.cs", "src/Changed.cs", "src/Gone.cs"],
            session.State.Files.Select(file => file.DisplayPath));
    }

    [Fact]
    public async Task It_counts_the_changes_by_the_kind_they_report()
    {
        // Arrange
        var session = SessionWith(
            new ChangedFile("/repo/src/Added.cs", "src/Added.cs", ChangeKind.Added),
            new ChangedFile("/repo/src/Changed.cs", "src/Changed.cs", ChangeKind.Modified),
            new ChangedFile("/repo/README.md", "README.md", ChangeKind.Modified),
            new ChangedFile("/repo/src/Gone.cs", "src/Gone.cs", ChangeKind.Deleted));

        // Act
        await session.LoadAsync("TerminalDotnet.slnx");

        // Assert
        Assert.Equal(new ChangesetSummary(2, 1, 1), session.State.Summary);
    }

    [Fact]
    public async Task It_selects_the_next_file_when_moving_down()
    {
        // Arrange
        var session = SessionWith(
            new ChangedFile("/repo/src/Added.cs", "src/Added.cs", ChangeKind.Added),
            new ChangedFile("/repo/src/Changed.cs", "src/Changed.cs", ChangeKind.Modified));
        await session.LoadAsync("TerminalDotnet.slnx");

        // Act
        await session.DispatchAsync(new ChangesetCommand.MoveDown());

        // Assert
        Assert.Equal("src/Changed.cs", session.State.Files[session.State.SelectedIndex].DisplayPath);
    }

    [Fact]
    public async Task It_shows_only_matching_files_when_searching()
    {
        // Arrange
        var session = SessionWith(
            new ChangedFile("/repo/src/OrderRepository.cs", "src/OrderRepository.cs", ChangeKind.Modified),
            new ChangedFile("/repo/src/Customer.cs", "src/Customer.cs", ChangeKind.Modified));
        await session.LoadAsync("TerminalDotnet.slnx");

        // Act
        await session.DispatchAsync(new ChangesetCommand.Search("orderrep"));

        // Assert
        Assert.Equal(
            ["src/OrderRepository.cs"],
            session.State.Files.Select(file => file.DisplayPath));
    }

    [Fact]
    public async Task It_ignores_files_that_only_scatter_the_query_across_their_path()
    {
        // Arrange
        var session = SessionWith(
            new ChangedFile(
                "/repo/src/Support/TestingExtensions.cs",
                "src/Support/TestingExtensions.cs",
                ChangeKind.Modified));
        await session.LoadAsync("TerminalDotnet.slnx");

        // Act
        await session.DispatchAsync(new ChangesetCommand.Search("appsettings"));

        // Assert
        Assert.Empty(session.State.Files);
    }

    [Fact]
    public async Task It_restores_every_file_when_the_search_is_cleared()
    {
        // Arrange
        var session = SessionWith(
            new ChangedFile("/repo/src/Order.cs", "src/Order.cs", ChangeKind.Modified),
            new ChangedFile("/repo/src/Customer.cs", "src/Customer.cs", ChangeKind.Modified));
        await session.LoadAsync("TerminalDotnet.slnx");
        await session.DispatchAsync(new ChangesetCommand.Search("order"));

        // Act
        await session.DispatchAsync(new ChangesetCommand.ClearSearch());

        // Assert
        Assert.Equal(2, session.State.Files.Count);
    }

    [Fact]
    public async Task It_loads_the_diff_of_the_selected_file()
    {
        // Arrange
        var session = SessionWith(
            new ChangedFile("/repo/src/Changed.cs", "src/Changed.cs", ChangeKind.Modified));
        await session.LoadAsync("TerminalDotnet.slnx");

        // Act
        await session.DispatchAsync(new ChangesetCommand.LoadSelectedDiff());

        // Assert
        Assert.Equal(
            new DiffContext("src/Changed.cs", "diff for src/Changed.cs"),
            session.State.Diff);
    }

    [Fact]
    public async Task It_drops_a_deleted_file_from_the_changeset_once_it_is_restored()
    {
        // Arrange
        var backend = new InMemoryChangesetBackend(
            new ChangedFile("/repo/src/Gone.cs", "src/Gone.cs", ChangeKind.Deleted));
        var session = new ChangesetSession(backend);
        await session.LoadAsync("TerminalDotnet.slnx");

        // Act
        await session.DispatchAsync(new ChangesetCommand.RestoreSelected());

        // Assert
        Assert.Empty(session.State.Files);
    }

    [Fact]
    public async Task It_leaves_a_file_that_was_not_deleted_alone()
    {
        // Arrange
        var backend = new InMemoryChangesetBackend(
            new ChangedFile("/repo/src/Changed.cs", "src/Changed.cs", ChangeKind.Modified));
        var session = new ChangesetSession(backend);
        await session.LoadAsync("TerminalDotnet.slnx");

        // Act
        await session.DispatchAsync(new ChangesetCommand.RestoreSelected());

        // Assert
        Assert.Empty(backend.Restored);
    }

    private static ChangesetSession SessionWith(params ChangedFile[] files) =>
        new(new InMemoryChangesetBackend(files));

    private sealed class InMemoryChangesetBackend(params ChangedFile[] files) : IChangesetBackend
    {
        private readonly List<ChangedFile> changedFiles = [.. files];

        public List<ChangedFile> Restored { get; } = [];

        public Task<IReadOnlyList<ChangedFile>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ChangedFile>>([.. changedFiles]);

        public Task<string> DiffAsync(ChangedFile file, CancellationToken cancellationToken = default) =>
            Task.FromResult($"diff for {file.DisplayPath}");

        public Task RestoreAsync(ChangedFile file, CancellationToken cancellationToken = default)
        {
            Restored.Add(file);
            changedFiles.Remove(file);
            return Task.CompletedTask;
        }
    }
}
