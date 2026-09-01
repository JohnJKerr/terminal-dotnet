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
        var unchanged = new FileEntry("App.csproj", "App", "Order.cs", FileGitStatus.Unchanged);
        var modified = unchanged with { GitStatus = FileGitStatus.Modified };
        var backend = new ChangingFileBackend(unchanged);
        var explorer = new FileExplorerSession(backend);
        await explorer.LoadAsync("App.csproj");
        var editor = new InMemoryFileOpener(() => backend.File = modified);
        var workflow = new ExplorerEditorWorkflow(explorer, editor, "App.csproj");

        // Act
        await workflow.OpenAsync("Order.cs", 1);

        // Assert
        Assert.Equal(FileGitStatus.Modified, explorer.State.VisibleNodes.Last().Files[0].GitStatus);
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
