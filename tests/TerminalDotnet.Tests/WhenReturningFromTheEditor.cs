using TerminalDotnet.Changes;
using TerminalDotnet.Files;
using TerminalDotnet.Terminal;
using TerminalDotnet.Tests.Builders;
using TerminalDotnet.Issues;
using TerminalDotnet.Tests.Fakes;
using Xunit;

namespace TerminalDotnet.Tests.Terminal;

public sealed class WhenReturningFromTheEditor
{
    [Fact]
    public async Task It_refreshes_the_explorers_git_status()
    {
        // Arrange
        var unchanged = new FileEntry("App.csproj", "Order.cs", FileGitStatus.Unchanged);
        var modified = unchanged with { GitStatus = FileGitStatus.Modified };
        var backend = new ChangingFileBackend(unchanged);
        var explorer = new FileExplorerSession(backend);
        await explorer.LoadAsync("App.csproj");
        var editor = new InMemoryFileOpener(() => backend.File = modified);
        var workflow = new ExplorerEditorWorkflow([explorer], Changeset(), editor, "App.csproj", Issues());

        // Act
        await workflow.OpenAsync("Order.cs", 1);
        await workflow.RefreshAsync(() => Task.CompletedTask);

        // Assert
        Assert.Equal(FileGitStatus.Modified, explorer.State.VisibleNodes.Last().Files[0].GitStatus);
    }

    [Fact]
    public async Task It_refreshes_the_changeset()
    {
        // Arrange
        var explorer = new FileExplorerSession(new ChangingFileBackend(
            new FileEntry("App.csproj", "Order.cs", FileGitStatus.Unchanged)));
        var changesetBackend = new ChangingChangesetBackend();
        var changes = new ChangesetSession(changesetBackend);
        await changes.LoadAsync("App.csproj");
        var editor = new InMemoryFileOpener(() => changesetBackend.Changed = true);
        var workflow = new ExplorerEditorWorkflow([explorer], changes, editor, "App.csproj", Issues());

        // Act
        await workflow.OpenAsync("Order.cs", 1);
        await workflow.RefreshAsync(() => Task.CompletedTask);

        // Assert
        Assert.Equal(["Order.cs"], changes.State.Files.Select(file => file.DisplayPath));
    }

    [Fact]
    public async Task It_refreshes_every_file_explorer_it_was_given()
    {
        // Arrange
        var unchanged = new FileEntry("App.csproj", "Order.cs", FileGitStatus.Unchanged);
        var backend = new ChangingFileBackend(unchanged);
        var folder = new FileExplorerSession(backend, FileGrouping.Folder);
        await folder.LoadAsync("App.csproj");
        var editor = new InMemoryFileOpener(
            () => backend.File = unchanged with { GitStatus = FileGitStatus.Modified });
        var workflow = new ExplorerEditorWorkflow(
            [new FileExplorerSession(backend), folder],
            Changeset(),
            editor,
            "App.csproj",
            Issues());

        // Act
        await workflow.OpenAsync("Order.cs", 1);
        await workflow.RefreshAsync(() => Task.CompletedTask);

        // Assert
        Assert.Equal(FileGitStatus.Modified, folder.State.VisibleNodes.Last().Files[0].GitStatus);
    }

    private static ChangesetSession Changeset() => new(new InMemoryChangesetBackend());

    private static IssueSession Issues() => GivenA.IssuePanel().Build();
}
