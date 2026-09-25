using TerminalDotnet.Changes;
using TerminalDotnet.Comments;
using TerminalDotnet.Explorer;
using TerminalDotnet.Files;
using TerminalDotnet.Flags;
using TerminalDotnet.Issues;
using TerminalDotnet.Terminal;
using TerminalDotnet.Testing;
using TerminalDotnet.Tests.Builders;
using Xunit;

namespace TerminalDotnet.Tests.Terminal;

public sealed class WhenFillingThePanels
{
    [Fact]
    public async Task It_loads_the_panels_in_the_order_they_are_listed()
    {
        // Arrange
        var loaded = new List<string>();
        var startup = StartupFor(loaded);

        // Act
        await startup.LoadPendingAsync();

        // Assert
        Assert.Equal(["files", "folder", "changes", "tests"], loaded);
    }

    [Fact]
    public async Task It_reports_after_every_panel_it_fills()
    {
        // Arrange
        var loaded = new List<string>();
        var reports = 0;
        var startup = StartupFor(loaded);

        // Act
        await startup.Panels.LoadPendingAsync(() =>
        {
            reports++;
            return Task.CompletedTask;
        });

        // Assert
        Assert.Equal(4, reports);
    }

    [Fact]
    public async Task It_leaves_a_panel_that_has_already_filled_alone()
    {
        // Arrange
        var loaded = new List<string>();
        var startup = StartupFor(loaded);
        await startup.LoadPendingAsync();
        loaded.Clear();

        // Act
        await startup.LoadPendingAsync();

        // Assert
        Assert.Empty(loaded);
    }

    [Fact]
    public async Task It_stops_at_the_panel_whose_load_was_cancelled()
    {
        // Arrange
        var loaded = new List<string>();
        var startup = StartupFor(loaded, cancelAfter: "changes");

        // Act
        await startup.LoadPendingUntilCancelledAsync();

        // Assert
        Assert.Equal(["files", "folder", "changes"], loaded);
    }

    [Fact]
    public async Task It_fills_the_panels_a_cancelled_load_left_behind()
    {
        // Arrange
        var loaded = new List<string>();
        var startup = StartupFor(loaded, cancelAfter: "changes");
        await startup.LoadPendingUntilCancelledAsync();
        loaded.Clear();

        // Act
        await startup.LoadPendingAsync();

        // Assert
        Assert.Equal(["changes", "tests"], loaded);
    }

    [Fact]
    public async Task It_fills_the_flags_among_the_issues()
    {
        // Arrange
        var issues = new IssueSession(new NoIssues(), new UnusedClipboard(), new OneFlag());
        var startup = new PanelStartup(
            new FileExplorerSession(new RecordingFileBackend(new Recorder([], new(), null), "files")),
            new FileExplorerSession(new RecordingFileBackend(new Recorder([], new(), null), "folder")),
            new ChangesetSession(new RecordingChangesetBackend(new Recorder([], new(), null))),
            GivenA.TestExplorer()
                .WithBackend(new RecordingTestBackend(new Recorder([], new(), null)))
                .Build(),
            "App.slnx",
            issues: issues);

        // Act
        await startup.LoadPendingAsync(() => Task.CompletedTask);

        // Assert
        Assert.Contains(issues.State.Issues, issue => issue.Severity == IssueSeverity.Flag);
    }

    private sealed class NoIssues : IIssueBackend
    {
        public Task<IReadOnlyList<CompilationIssue>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CompilationIssue>>([]);
    }

    private sealed class OneFlag : IFlagBackend
    {
        public Task<IReadOnlyList<Flag>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Flag>>([new("/repo/Work.cs", "Work.cs", 1, FlagKind.Todo, "later")]);
    }

    private sealed class UnusedClipboard : ICommentClipboard
    {
        public Task<bool> TryCopyAsync(string text, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }

    private sealed record Startup(PanelStartup Panels, CancellationToken Cancelled)
    {
        public Task LoadPendingAsync() => Panels.LoadPendingAsync(() => Task.CompletedTask);

        public Task LoadPendingUntilCancelledAsync() =>
            Panels.LoadPendingAsync(() => Task.CompletedTask, Cancelled);
    }

    private static Startup StartupFor(List<string> loaded, string? cancelAfter = null)
    {
        var cancellation = new CancellationTokenSource();
        var recorder = new Recorder(loaded, cancellation, cancelAfter);
        return new Startup(
            new PanelStartup(
                new FileExplorerSession(new RecordingFileBackend(recorder, "files")),
                new FileExplorerSession(
                    new RecordingFileBackend(recorder, "folder"),
                    FileGrouping.Folder),
                new ChangesetSession(new RecordingChangesetBackend(recorder)),
                GivenA.TestExplorer()
                    .WithBackend(new RecordingTestBackend(recorder))
                    .Build(),
                "App.slnx"),
            cancellation.Token);
    }

    private sealed class Recorder(
        List<string> loaded,
        CancellationTokenSource cancellation,
        string? cancelAfter)
    {
        private string? pendingCancel = cancelAfter;

        public void Record(string panel)
        {
            loaded.Add(panel);
            if (pendingCancel != panel)
            {
                return;
            }

            pendingCancel = null;
            cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }
    }

    private sealed class RecordingFileBackend(Recorder recorder, string panel) : IFileExplorerBackend
    {
        public Task<IReadOnlyList<FileEntry>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default)
        {
            recorder.Record(panel);
            return Task.FromResult<IReadOnlyList<FileEntry>>(
                [new FileEntry("App.csproj", "Order.cs", FileGitStatus.Unchanged)]);
        }
    }

    private sealed class RecordingChangesetBackend(Recorder recorder) : IChangesetBackend
    {
        public Task<IReadOnlyList<ChangedFile>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default)
        {
            recorder.Record("changes");
            return Task.FromResult<IReadOnlyList<ChangedFile>>([]);
        }

        public Task<string> DiffAsync(ChangedFile file, CancellationToken cancellationToken = default) =>
            Task.FromResult("");

        public Task<bool> RestoreAsync(ChangedFile file, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    private sealed class RecordingTestBackend(Recorder recorder) : ITestBackend
    {
        public Task<IReadOnlyList<TestCase>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default)
        {
            recorder.Record("tests");
            return Task.FromResult<IReadOnlyList<TestCase>>(
                [GivenA.TestCase("Shop.Tests.CartTests.Adds_item")]);
        }

        public Task<TestRun> RunAsync(
            IReadOnlyCollection<TestCase> tests,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new TestRun(true, "Passed"));
    }
}
