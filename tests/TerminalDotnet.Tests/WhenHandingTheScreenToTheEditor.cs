using TerminalDotnet.Changes;
using TerminalDotnet.Comments;
using TerminalDotnet.Files;
using TerminalDotnet.Flags;
using TerminalDotnet.Issues;
using TerminalDotnet.Terminal;
using TerminalDotnet.Tests.Builders;
using TerminalDotnet.Tests.Fakes;
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
        var issues = GivenA.IssuePanel().WithFlagBackend(flagBackend).Build();
        await issues.LoadFlagsAsync("App.csproj");
        var editor = new InMemoryFileOpener(() => flagBackend.Flagged = true);
        var workflow = Workflow(new FileExplorerSession(Unchanging), editor, issues: issues);

        // Act
        await workflow.OpenAsync("Order.cs", 1);
        await workflow.RefreshAsync(() => Task.CompletedTask);

        // Assert
        Assert.Contains(issues.State.Issues, issue => issue.Severity == IssueSeverity.Flag);
    }

    [Fact]
    public async Task It_refreshes_the_compilation_issues()
    {
        // Arrange
        var issueBackend = new GrowingIssueBackend();
        var issues = GivenA.IssuePanel().WithIssueBackend(issueBackend).Build();
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
        var workflow = Workflow(new FileExplorerSession(Unchanging), new InMemoryFileOpener(() => { }));

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
        IssueSession? issues = null) =>
        new([explorer],
            new ChangesetSession(new InMemoryChangesetBackend()),
            editor,
            "App.csproj",
            issues ?? GivenA.IssuePanel().Build());

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
}
