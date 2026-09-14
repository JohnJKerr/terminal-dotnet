using TerminalDotnet.Comments;
using TerminalDotnet.Explorer;
using TerminalDotnet.Issues;
using TerminalDotnet.Terminal;
using TerminalDotnet.Testing;
using Xunit;

namespace TerminalDotnet.Tests.Terminal;

/// <summary>A rebuild answers two questions: does it compile, and what tests
/// does it hold. Discovery builds too, so the two run one after the other
/// rather than as two builds fighting over the same output.</summary>
public sealed class WhenRebuildingTheProject
{
    private static readonly TestCase Adds =
        new("Shop.Tests.CartTests.Adds_item", "Adds item", "Shop.Tests.csproj");

    private static readonly CompilationIssue Broken =
        new("/repo/Cart.cs", "Cart.cs", 3, 1, "CS1002", "; expected", IssueSeverity.Error);

    private static readonly CompilationIssue Unused =
        new("/repo/Cart.cs", "Cart.cs", 9, 5, "CS0168", "unused", IssueSeverity.Warning);

    [Fact]
    public async Task It_builds_the_issues()
    {
        // Arrange
        var log = new List<string>();
        var rebuild = Rebuild(log, [Unused]);

        // Act
        await rebuild.RunAsync(() => Task.CompletedTask);

        // Assert
        Assert.Contains("build", log);
    }

    [Fact]
    public async Task It_discovers_the_tests_once_the_build_has_finished()
    {
        // Arrange
        var log = new List<string>();
        var rebuild = Rebuild(log, [Unused]);

        // Act
        await rebuild.RunAsync(() => Task.CompletedTask);

        // Assert
        Assert.Equal(["build", "discover"], log);
    }

    [Fact]
    public async Task It_leaves_the_tests_alone_when_the_build_is_broken()
    {
        // Arrange
        var log = new List<string>();
        var rebuild = Rebuild(log, [Broken]);

        // Act
        await rebuild.RunAsync(() => Task.CompletedTask);

        // Assert
        Assert.DoesNotContain("discover", log);
    }

    [Fact]
    public async Task It_reports_each_panel_as_it_lands()
    {
        // Arrange
        var landed = 0;
        var rebuild = Rebuild([], [Unused]);

        // Act
        await rebuild.RunAsync(() =>
        {
            landed++;
            return Task.CompletedTask;
        });

        // Assert
        Assert.Equal(2, landed);
    }

    [Fact]
    public async Task It_marks_both_panels_as_working_before_either_starts()
    {
        // Arrange
        var log = new List<string>();
        var issues = new IssueSession(new LoggingIssueBackend(log, []), new SilentClipboard());
        var tests = new TestExplorerSession(new LoggingTestBackend(log));
        await issues.LoadAsync("Shop.sln");
        await tests.LoadAsync("Shop.sln");

        // Act
        new ProjectRebuild(issues, tests, "Shop.sln").Start();

        // Assert
        Assert.Equal(ExplorerStatus.Loading, tests.State.Status);
    }

    [Fact]
    public async Task A_broken_build_puts_the_tests_back_as_they_were()
    {
        // Arrange
        var issues = new IssueSession(new LoggingIssueBackend([], [Broken]), new SilentClipboard());
        var tests = new TestExplorerSession(new LoggingTestBackend([]));
        await issues.LoadAsync("Shop.sln");
        await tests.LoadAsync("Shop.sln");
        var rebuild = new ProjectRebuild(issues, tests, "Shop.sln");
        rebuild.Start();

        // Act
        await rebuild.RunAsync(() => Task.CompletedTask);

        // Assert
        Assert.Equal(ExplorerStatus.Ready, tests.State.Status);
    }

    [Fact]
    public async Task It_will_not_start_while_the_tests_are_running()
    {
        // Arrange
        var issues = new IssueSession(new LoggingIssueBackend([], []), new SilentClipboard());
        var tests = new TestExplorerSession(new RunningTestBackend());
        await issues.LoadAsync("Shop.sln");
        await tests.LoadAsync("Shop.sln");
        _ = tests.DispatchAsync(new ExplorerCommand.RunSelected());

        // Act
        var started = new ProjectRebuild(issues, tests, "Shop.sln").Start();

        // Assert
        Assert.False(started);
    }

    private sealed class RunningTestBackend : ITestBackend
    {
        private readonly TaskCompletionSource<TestRun> never = new();

        public Task<IReadOnlyList<TestCase>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TestCase>>([Adds]);

        public Task<TestRun> RunAsync(
            IReadOnlyCollection<TestCase> tests,
            CancellationToken cancellationToken = default) =>
            never.Task;
    }

    private static ProjectRebuild Rebuild(List<string> log, IReadOnlyList<CompilationIssue> found) => new(
        new IssueSession(new LoggingIssueBackend(log, found), new SilentClipboard()),
        new TestExplorerSession(new LoggingTestBackend(log)),
        "Shop.sln");

    private sealed class LoggingIssueBackend(List<string> log, IReadOnlyList<CompilationIssue> found)
        : IIssueBackend
    {
        public Task<IReadOnlyList<CompilationIssue>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default)
        {
            log.Add("build");
            return Task.FromResult(found);
        }
    }

    private sealed class LoggingTestBackend(List<string> log) : ITestBackend
    {
        public Task<IReadOnlyList<TestCase>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default)
        {
            log.Add("discover");
            return Task.FromResult<IReadOnlyList<TestCase>>([Adds]);
        }

        public Task<TestRun> RunAsync(
            IReadOnlyCollection<TestCase> tests,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class SilentClipboard : ICommentClipboard
    {
        public Task<bool> TryCopyAsync(string text, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }
}
