using TerminalDotnet.Changes;
using TerminalDotnet.Files;
using TerminalDotnet.Terminal;
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
        var workflow = new ExplorerEditorWorkflow(explorer, Changeset(), editor, "App.csproj");

        // Act
        await workflow.OpenAsync("Order.cs", 1);

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
        var workflow = new ExplorerEditorWorkflow(explorer, changes, editor, "App.csproj");

        // Act
        await workflow.OpenAsync("Order.cs", 1);

        // Assert
        Assert.Equal(["Order.cs"], changes.State.Files.Select(file => file.DisplayPath));
    }

    private static ChangesetSession Changeset() => new(new EmptyChangesetBackend());

    private sealed class ChangingChangesetBackend : IChangesetBackend
    {
        public bool Changed { get; set; }

        public Task<IReadOnlyList<ChangedFile>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ChangedFile>>(Changed
                ? [new ChangedFile("/repo/Order.cs", "Order.cs", ChangeKind.Modified)]
                : []);

        public Task<string> DiffAsync(ChangedFile file, CancellationToken cancellationToken = default) =>
            Task.FromResult("");

        public Task RestoreAsync(ChangedFile file, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class EmptyChangesetBackend : IChangesetBackend
    {
        public Task<IReadOnlyList<ChangedFile>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ChangedFile>>([]);

        public Task<string> DiffAsync(ChangedFile file, CancellationToken cancellationToken = default) =>
            Task.FromResult("");

        public Task RestoreAsync(ChangedFile file, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class ChangingFileBackend(FileEntry file) : IFileExplorerBackend
    {
        public FileEntry File { get; set; } = file;

        public Task<IReadOnlyList<FileEntry>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FileEntry>>([File]);
    }

    private sealed class InMemoryFileOpener(Action onOpen) : IFileOpener
    {
        public Task OpenAsync(string path, int line, CancellationToken cancellationToken = default)
        {
            onOpen();
            return Task.CompletedTask;
        }
    }
}
