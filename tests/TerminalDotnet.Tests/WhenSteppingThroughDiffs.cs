using TerminalDotnet.Changes;
using Xunit;

namespace TerminalDotnet.Tests.Changeset;

public sealed class WhenSteppingThroughDiffs
{
    [Fact]
    public async Task It_shows_the_diff_of_the_file_after_the_selection()
    {
        // Arrange
        var session = await LoadedSession();

        // Act
        await session.DispatchAsync(new ChangesetCommand.StepDiff(1));

        // Assert
        Assert.Equal("src/Changed.cs", session.State.Diff?.DisplayPath);
    }

    [Fact]
    public async Task It_shows_the_diff_of_the_file_before_the_selection()
    {
        // Arrange
        var session = await LoadedSession();

        // Act
        await session.DispatchAsync(new ChangesetCommand.StepDiff(-1));

        // Assert
        Assert.Equal("src/Gone.cs", session.State.Diff?.DisplayPath);
    }

    [Fact]
    public async Task It_wraps_to_the_first_file_past_the_last_one()
    {
        // Arrange
        var session = await LoadedSession();
        await session.DispatchAsync(new ChangesetCommand.SelectIndex(2));

        // Act
        await session.DispatchAsync(new ChangesetCommand.StepDiff(1));

        // Assert
        Assert.Equal("src/Added.cs", session.State.Diff?.DisplayPath);
    }

    [Fact]
    public async Task It_takes_the_panel_selection_with_it()
    {
        // Arrange
        var session = await LoadedSession();

        // Act
        await session.DispatchAsync(new ChangesetCommand.StepDiff(1));

        // Assert
        Assert.Equal(1, session.State.SelectedIndex);
    }

    [Fact]
    public async Task It_steps_through_the_files_the_search_left()
    {
        // Arrange
        var session = await LoadedSession();
        await session.DispatchAsync(new ChangesetCommand.Search("gone"));

        // Act
        await session.DispatchAsync(new ChangesetCommand.StepDiff(1));

        // Assert
        Assert.Equal("src/Gone.cs", session.State.Diff?.DisplayPath);
    }

    [Fact]
    public async Task It_shows_nothing_new_without_a_file_to_step_to()
    {
        // Arrange
        var session = new ChangesetSession(new InMemoryChangesetBackend());
        await session.LoadAsync("TerminalDotnet.slnx");

        // Act
        await session.DispatchAsync(new ChangesetCommand.StepDiff(1));

        // Assert
        Assert.Null(session.State.Diff);
    }

    private static async Task<ChangesetSession> LoadedSession()
    {
        var session = new ChangesetSession(new InMemoryChangesetBackend(
            new ChangedFile("/repo/src/Added.cs", "src/Added.cs", ChangeKind.Added),
            new ChangedFile("/repo/src/Changed.cs", "src/Changed.cs", ChangeKind.Modified),
            new ChangedFile("/repo/src/Gone.cs", "src/Gone.cs", ChangeKind.Deleted)));
        await session.LoadAsync("TerminalDotnet.slnx");
        return session;
    }

    private sealed class InMemoryChangesetBackend(params ChangedFile[] files) : IChangesetBackend
    {
        public Task<IReadOnlyList<ChangedFile>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ChangedFile>>([.. files]);

        public Task<string> DiffAsync(ChangedFile file, CancellationToken cancellationToken = default) =>
            Task.FromResult($"diff for {file.DisplayPath}");

        public Task<bool> RestoreAsync(ChangedFile file, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }
}
