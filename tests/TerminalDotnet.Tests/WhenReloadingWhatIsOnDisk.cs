using TerminalDotnet.Changes;
using TerminalDotnet.Comments;
using TerminalDotnet.Files;
using TerminalDotnet.Flags;
using TerminalDotnet.Issues;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Terminal;

public sealed class WhenReloadingWhatIsOnDisk
{
    [Fact]
    public async Task It_picks_up_the_files_that_changed()
    {
        // Arrange
        var backend = new ChangingFileBackend(
            new FileEntry("App.csproj", "Order.cs", FileGitStatus.Unchanged));
        var explorer = new FileExplorerSession(backend);
        await explorer.LoadAsync("App.csproj");
        backend.File = backend.File with { GitStatus = FileGitStatus.Modified };

        // Act
        await Reload(explorers: [explorer]).FromDiskAsync(() => Task.CompletedTask);

        // Assert
        Assert.Equal(FileGitStatus.Modified, explorer.State.VisibleNodes.Last().Files[0].GitStatus);
    }

    [Fact]
    public async Task It_picks_up_the_changeset()
    {
        // Arrange
        var changesetBackend = new ChangingChangesetBackend();
        var changes = new ChangesetSession(changesetBackend);
        await changes.LoadAsync("App.csproj");
        changesetBackend.Changed = true;

        // Act
        await Reload(changes: changes).FromDiskAsync(() => Task.CompletedTask);

        // Assert
        Assert.Equal(["Order.cs"], changes.State.Files.Select(file => file.DisplayPath));
    }

    [Fact]
    public async Task It_picks_up_the_flags()
    {
        // Arrange
        var flagBackend = new CountingFlagBackend();
        var issues = new IssueSession(new CountingIssueBackend(), new SilentClipboard(), flagBackend);

        // Act
        await Reload(issues: issues).FromDiskAsync(() => Task.CompletedTask);

        // Assert
        Assert.Equal(1, flagBackend.Discoveries);
    }

    [Fact]
    public async Task It_leaves_the_build_behind_the_issues_alone()
    {
        // Arrange
        var issueBackend = new CountingIssueBackend();
        var issues = new IssueSession(issueBackend, new SilentClipboard());

        // Act
        await Reload(issues: issues).FromDiskAsync(() => Task.CompletedTask);

        // Assert
        Assert.Equal(0, issueBackend.Discoveries);
    }

    [Fact]
    public async Task Reloading_everything_builds_the_issues()
    {
        // Arrange
        var issueBackend = new CountingIssueBackend();
        var issues = new IssueSession(issueBackend, new SilentClipboard());

        // Act
        await Reload(issues: issues).EverythingAsync(() => Task.CompletedTask);

        // Assert
        Assert.Equal(1, issueBackend.Discoveries);
    }

    [Fact]
    public async Task It_reports_each_panel_as_it_lands()
    {
        // Arrange
        var explorer = new FileExplorerSession(new ChangingFileBackend(
            new FileEntry("App.csproj", "Order.cs", FileGitStatus.Unchanged)));
        var landed = 0;

        // Act
        await Reload(
                explorers: [explorer],
                issues: new IssueSession(new CountingIssueBackend(), new SilentClipboard(), new CountingFlagBackend()))
            .FromDiskAsync(() =>
            {
                landed++;
                return Task.CompletedTask;
            });

        // Assert
        Assert.Equal(3, landed);
    }

    private static PanelReload Reload(
        IReadOnlyList<FileExplorerSession>? explorers = null,
        ChangesetSession? changes = null,
        IssueSession? issues = null) =>
        new(explorers ?? [],
            changes ?? new ChangesetSession(new ChangingChangesetBackend()),
            "App.csproj",
            issues);

    private sealed class CountingIssueBackend : IIssueBackend
    {
        public int Discoveries { get; private set; }

        public Task<IReadOnlyList<CompilationIssue>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default)
        {
            Discoveries++;
            return Task.FromResult<IReadOnlyList<CompilationIssue>>([]);
        }
    }

    private sealed class CountingFlagBackend : IFlagBackend
    {
        public int Discoveries { get; private set; }

        public Task<IReadOnlyList<Flag>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default)
        {
            Discoveries++;
            return Task.FromResult<IReadOnlyList<Flag>>([]);
        }
    }

    private sealed class SilentClipboard : ICommentClipboard
    {
        public Task<bool> TryCopyAsync(string text, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

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
}
