using TerminalDotnet.Changes;
using TerminalDotnet.Explorer;
using TerminalDotnet.Files;
using TerminalDotnet.Testing;
using TerminalDotnet.Tests.Builders;
using Xunit;

namespace TerminalDotnet.Tests.Explorer;

public sealed class WhenAPanelPublishesItsState
{
    [Fact]
    public async Task It_keeps_the_changes_it_discovered_when_the_backend_changes_its_list()
    {
        // Arrange
        var discovered = new List<ChangedFile> { Changed };
        var session = new ChangesetSession(new MutableChangesetBackend(discovered));
        await session.LoadAsync("App.slnx");

        // Act
        discovered.Clear();

        // Assert
        Assert.Equal(["src/Changed.cs"], session.State.Files.Select(file => file.DisplayPath));
    }

    [Fact]
    public async Task It_publishes_changes_that_cannot_be_replaced()
    {
        // Arrange
        var session = new ChangesetSession(new MutableChangesetBackend([Changed]));
        await session.LoadAsync("App.slnx");

        // Act
        var files = (IList<ChangedFile>)session.State.Files;

        // Assert
        Assert.Throws<NotSupportedException>(() => files[0] = Changed with { DisplayPath = "other" });
    }

    [Fact]
    public async Task It_keeps_the_files_it_discovered_when_the_backend_changes_its_list()
    {
        // Arrange
        var discovered = new List<FileEntry> { Order };
        var session = new FileExplorerSession(new MutableFileBackend(discovered));
        await session.LoadAsync("/repo/App.csproj");

        // Act
        discovered.Clear();

        // Assert
        Assert.NotEmpty(session.State.VisibleNodes);
    }

    [Fact]
    public async Task It_publishes_file_nodes_that_cannot_be_replaced()
    {
        // Arrange
        var session = new FileExplorerSession(new MutableFileBackend([Order]));
        await session.LoadAsync("/repo/App.csproj");

        // Act
        var nodes = (IList<VisibleFileNode>)session.State.VisibleNodes;

        // Assert
        Assert.Throws<NotSupportedException>(() => nodes[0] = nodes[0] with { Name = "other" });
    }

    [Fact]
    public async Task It_keeps_the_tests_it_discovered_when_the_backend_changes_its_list()
    {
        // Arrange
        var discovered = new List<TestCase> { AddsItem };
        var session = await GivenA.TestExplorer()
            .WithBackend(new MutableTestBackend(discovered))
            .LoadedAsync();

        // Act
        discovered.Clear();

        // Assert
        Assert.NotEmpty(session.State.VisibleNodes);
    }

    [Fact]
    public async Task It_publishes_test_membership_that_cannot_be_replaced()
    {
        // Arrange
        var session = await GivenA.TestExplorer()
            .WithBackend(new MutableTestBackend([AddsItem]))
            .LoadedAsync();

        // Act
        var tests = (IList<TestCase>)session.State.VisibleNodes[0].Tests;

        // Assert
        Assert.Throws<NotSupportedException>(() => tests[0] = AddsItem with { DisplayName = "other" });
    }

    [Fact]
    public async Task It_publishes_run_results_that_cannot_be_replaced()
    {
        // Arrange
        var run = new TestRun(true, "Passed", [PassedResult]);
        var session = await GivenA.TestExplorer()
            .WithBackend(new MutableTestBackend([AddsItem], run))
            .LoadedAsync();
        await session.DispatchAsync(new ExplorerCommand.RunSelected());

        // Act
        var results = (IList<TestResult>)session.State.LastRun!.Results;

        // Assert
        Assert.Throws<NotSupportedException>(() =>
            results[0] = PassedResult with { Outcome = TestOutcome.Failed });
    }

    private static readonly ChangedFile Changed =
        new("/repo/src/Changed.cs", "src/Changed.cs", ChangeKind.Modified);

    private static readonly FileEntry Order =
        new("/repo/App.csproj", "/repo/Order.cs", FileGitStatus.Unchanged);

    private static readonly TestCase AddsItem =
        new("Shop.Tests.CartTests.Adds_item", "Adds item", "Shop.Tests.csproj");

    private static readonly TestResult PassedResult =
        new(AddsItem, TestOutcome.Passed, TimeSpan.Zero, null, null, null, null);

    private sealed class MutableChangesetBackend(List<ChangedFile> files) : IChangesetBackend
    {
        public Task<IReadOnlyList<ChangedFile>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ChangedFile>>(files);

        public Task<string> DiffAsync(ChangedFile file, CancellationToken cancellationToken = default) =>
            Task.FromResult("");

        public Task<bool> RestoreAsync(ChangedFile file, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    private sealed class MutableFileBackend(List<FileEntry> files) : IFileExplorerBackend
    {
        public Task<IReadOnlyList<FileEntry>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FileEntry>>(files);
    }

    private sealed class MutableTestBackend(List<TestCase> tests, TestRun? run = null) : ITestBackend
    {
        public Task<IReadOnlyList<TestCase>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TestCase>>(tests);

        public Task<TestRun> RunAsync(
            IReadOnlyCollection<TestCase> requested,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(run ?? new TestRun(true, "Passed"));
    }
}
