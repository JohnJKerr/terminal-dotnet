using TerminalDotnet.Changes;
using TerminalDotnet.Explorer;
using TerminalDotnet.Files;
using TerminalDotnet.Terminal;
using TerminalDotnet.Testing;
using Xunit;

namespace TerminalDotnet.Tests.Terminal;

public sealed class WhenFillingThePanels
{
    [Fact]
    public async Task It_loads_the_files_the_changes_and_then_the_tests()
    {
        // Arrange
        var loaded = new List<string>();
        var startup = StartupFor(loaded);

        // Act
        await startup.LoadPendingAsync();

        // Assert
        Assert.Equal(["files", "changes", "tests"], loaded);
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
        Assert.Equal(3, reports);
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
        Assert.Equal(["files", "changes"], loaded);
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
                new FileExplorerSession(new RecordingFileBackend(recorder)),
                new ChangesetSession(new RecordingChangesetBackend(recorder)),
                new TestExplorerSession(new RecordingTestBackend(recorder)),
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

    private sealed class RecordingFileBackend(Recorder recorder) : IFileExplorerBackend
    {
        public Task<IReadOnlyList<FileEntry>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default)
        {
            recorder.Record("files");
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
                [new TestCase("Shop.Tests.CartTests.Adds_item", "Adds item", "Shop.Tests.csproj")]);
        }

        public Task<TestRun> RunAsync(
            IReadOnlyCollection<TestCase> tests,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new TestRun(true, "Passed"));
    }
}
