using TerminalDotnet.Changes;
using TerminalDotnet.Comments;
using TerminalDotnet.Files;
using TerminalDotnet.Flags;
using TerminalDotnet.Issues;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Terminal;

public sealed class WhenHandingTheScreenToTheEditor
{
    [Fact]
    public async Task It_leaves_the_panels_alone_while_the_editor_holds_the_screen()
    {
        // Arrange
        var backend = new ChangingFileBackend(Unchanged);
        var explorer = new FileExplorerSession(backend);
        await explorer.LoadAsync("App.csproj");
        var editor = new InMemoryFileOpener(() => backend.File = Modified);
        var workflow = Workflow(explorer, editor);

        // Act
        await workflow.OpenAsync("Order.cs", 1);

        // Assert
        Assert.Equal(FileGitStatus.Unchanged, explorer.State.VisibleNodes.Last().Files[0].GitStatus);
    }

    [Fact]
    public async Task It_refreshes_the_flags()
    {
        // Arrange
        var flagBackend = new GrowingFlagBackend();
        var flags = new FlagSession(flagBackend);
        await flags.LoadAsync("App.csproj");
        var editor = new InMemoryFileOpener(() => flagBackend.Flagged = true);
        var workflow = Workflow(new FileExplorerSession(Unchanging), editor, flags: flags);

        // Act
        await workflow.OpenAsync("Order.cs", 1);
        await workflow.RefreshAsync(() => Task.CompletedTask);

        // Assert
        Assert.Single(flags.State.Flags);
    }

    [Fact]
    public async Task It_refreshes_the_compilation_issues()
    {
        // Arrange
        var issueBackend = new GrowingIssueBackend();
        var issues = new IssueSession(issueBackend, new UnusedClipboard());
        await issues.LoadAsync("App.csproj");
        var editor = new InMemoryFileOpener(() => issueBackend.Broken = true);
        var workflow = Workflow(new FileExplorerSession(Unchanging), editor, issues: issues);

        // Act
        await workflow.OpenAsync("Order.cs", 1);
        await workflow.RefreshAsync(() => Task.CompletedTask);

        // Assert
        Assert.Single(issues.State.Issues);
    }

    [Fact]
    public async Task It_reports_every_panel_as_it_lands()
    {
        // Arrange
        var landed = 0;
        var workflow = Workflow(
            new FileExplorerSession(Unchanging),
            new InMemoryFileOpener(() => { }),
            flags: new FlagSession(new GrowingFlagBackend()),
            issues: new IssueSession(new GrowingIssueBackend(), new UnusedClipboard()));

        // Act
        await workflow.RefreshAsync(() =>
        {
            landed++;
            return Task.CompletedTask;
        });

        // Assert
        Assert.Equal(4, landed);
    }

    private static FileEntry Unchanged => new("App.csproj", "Order.cs", FileGitStatus.Unchanged);

    private static FileEntry Modified => Unchanged with { GitStatus = FileGitStatus.Modified };

    private static IFileExplorerBackend Unchanging => new ChangingFileBackend(Unchanged);

    private static ExplorerEditorWorkflow Workflow(
        FileExplorerSession explorer,
        IFileOpener editor,
        FlagSession? flags = null,
        IssueSession? issues = null) =>
        new([explorer],
            new ChangesetSession(new EmptyChangesetBackend()),
            editor,
            "App.csproj",
            flags,
            issues);

    private sealed class GrowingFlagBackend : IFlagBackend
    {
        public bool Flagged { get; set; }

        public Task<IReadOnlyList<Flag>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Flag>>(Flagged
                ? [new Flag("/repo/Order.cs", "Order.cs", 3, FlagKind.Todo, "Todo: split this")]
                : []);
    }

    private sealed class GrowingIssueBackend : IIssueBackend
    {
        public bool Broken { get; set; }

        public Task<IReadOnlyList<CompilationIssue>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CompilationIssue>>(Broken
                ? [new CompilationIssue(
                    "/repo/Order.cs",
                    "Order.cs",
                    3,
                    9,
                    "CS0103",
                    "The name 'total' does not exist",
                    IssueSeverity.Error)]
                : []);
    }

    private sealed class EmptyChangesetBackend : IChangesetBackend
    {
        public Task<IReadOnlyList<ChangedFile>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ChangedFile>>([]);

        public Task<string> DiffAsync(ChangedFile file, CancellationToken cancellationToken = default) =>
            Task.FromResult("");

        public Task<bool> RestoreAsync(ChangedFile file, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
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

    private sealed class UnusedClipboard : ICommentClipboard
    {
        public Task<bool> TryCopyAsync(string text, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }
}
