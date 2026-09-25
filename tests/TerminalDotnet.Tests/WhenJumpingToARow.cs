using TerminalDotnet.Changes;
using TerminalDotnet.Comments;
using TerminalDotnet.Explorer;
using TerminalDotnet.Files;
using TerminalDotnet.Testing;
using TerminalDotnet.Tests.Builders;
using TerminalDotnet.Tests.Fakes;
using Xunit;

namespace TerminalDotnet.Tests.Explorer;

public sealed class WhenJumpingToARow
{
    [Fact]
    public async Task It_selects_the_row_it_was_sent_to()
    {
        // Arrange
        var session = await SessionWithThreeFiles();

        // Act
        await session.DispatchAsync(new FileExplorerCommand.SelectIndex(2));

        // Assert
        Assert.Equal(2, session.State.SelectedIndex);
    }

    [Fact]
    public async Task It_stays_on_the_last_row_when_sent_past_the_end()
    {
        // Arrange
        var session = await SessionWithThreeFiles();

        // Act
        await session.DispatchAsync(new FileExplorerCommand.SelectIndex(9));

        // Assert
        Assert.Equal(2, session.State.SelectedIndex);
    }

    [Fact]
    public async Task It_stays_on_the_first_row_when_sent_before_the_start()
    {
        // Arrange
        var session = await SessionWithThreeFiles();
        await session.DispatchAsync(new FileExplorerCommand.SelectIndex(2));

        // Act
        await session.DispatchAsync(new FileExplorerCommand.SelectIndex(-1));

        // Assert
        Assert.Equal(0, session.State.SelectedIndex);
    }

    [Fact]
    public async Task It_selects_the_changed_file_it_was_sent_to()
    {
        // Arrange
        var session = new ChangesetSession(new InMemoryChangesetBackend(
            new ChangedFile("/repo/src/Added.cs", "src/Added.cs", ChangeKind.Added),
            new ChangedFile("/repo/src/Changed.cs", "src/Changed.cs", ChangeKind.Modified)));
        await session.LoadAsync("TerminalDotnet.slnx");

        // Act
        await session.DispatchAsync(new ChangesetCommand.SelectIndex(1));

        // Assert
        Assert.Equal("src/Changed.cs", session.State.Files[session.State.SelectedIndex].DisplayPath);
    }

    [Fact]
    public async Task It_stays_on_the_last_changed_file_when_sent_past_the_end()
    {
        // Arrange
        var session = new ChangesetSession(new InMemoryChangesetBackend(
            new ChangedFile("/repo/src/Added.cs", "src/Added.cs", ChangeKind.Added)));
        await session.LoadAsync("TerminalDotnet.slnx");

        // Act
        await session.DispatchAsync(new ChangesetCommand.SelectIndex(9));

        // Assert
        Assert.Equal(0, session.State.SelectedIndex);
    }

    [Fact]
    public async Task It_selects_the_commented_file_it_was_sent_to()
    {
        // Arrange
        var session = await GivenA.CommentSession().WithTwoNotes().BuildAsync();

        // Act
        await session.DispatchAsync(new CommentCommand.SelectIndex(1));

        // Assert
        Assert.Equal(
            "src/Order.cs",
            session.State.Comments[session.State.SelectedIndex].DisplayPath);
    }

    [Fact]
    public async Task It_stays_on_the_last_commented_file_when_sent_past_the_end()
    {
        // Arrange
        var session = await GivenA.CommentSession().WithNote("src/Order.cs", "needs a guard").BuildAsync();

        // Act
        await session.DispatchAsync(new CommentCommand.SelectIndex(9));

        // Assert
        Assert.Equal(0, session.State.SelectedIndex);
    }

    private static async Task<FileExplorerSession> SessionWithThreeFiles()
    {
        var session = new FileExplorerSession(new FixedFiles(
            new FileEntry("src/App/App.csproj", "src/App/Order.cs", FileGitStatus.Unchanged),
            new FileEntry("src/App/App.csproj", "src/App/Customer.cs", FileGitStatus.Unchanged)));
        await session.LoadAsync("TerminalDotnet.slnx");
        return session;
    }

    [Fact]
    public async Task It_selects_the_test_row_it_was_sent_to()
    {
        // Arrange
        var session = await SessionWithTwoTests();

        // Act
        await session.DispatchAsync(new ExplorerCommand.SelectIndex(2));

        // Assert
        Assert.Equal(2, session.State.SelectedIndex);
    }

    [Fact]
    public async Task It_stays_on_the_last_test_row_when_sent_past_the_end()
    {
        // Arrange
        var session = await SessionWithTwoTests();

        // Act
        await session.DispatchAsync(new ExplorerCommand.SelectIndex(99));

        // Assert
        Assert.Equal(3, session.State.SelectedIndex);
    }

    private static Task<TestExplorerSession> SessionWithTwoTests() => GivenA.TestExplorer()
        .WithTests(
            GivenA.TestCase("Shop.Tests.CartTests.Adds_item"),
            GivenA.TestCase("Shop.Tests.CartTests.Drops_item"))
        .LoadedAsync();
}
