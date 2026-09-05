using TerminalDotnet.Changes;
using TerminalDotnet.Explorer;
using TerminalDotnet.Filters;
using TerminalDotnet.Testing;
using Xunit;

namespace TerminalDotnet.Tests.Testing;

public sealed class WhenFilteringToUpdatedTests
{
    [Fact]
    public async Task It_keeps_only_the_suites_whose_source_changed()
    {
        // Arrange
        var session = await LoadedSessionAsync(new InMemoryUpdatedSource(["/repo/Shop.Tests/CartTests.cs"]));

        // Act
        await session.DispatchAsync(new ExplorerCommand.ToggleFilter(ExplorerFilter.Updated));

        // Assert
        Assert.Equal(
            ["Shop.Tests", "CartTests", "Adds item"],
            session.State.VisibleNodes.Select(node => node.Name));
    }

    [Fact]
    public async Task It_remembers_the_filter_it_is_using()
    {
        // Arrange
        var session = await LoadedSessionAsync(new InMemoryUpdatedSource(["/repo/Shop.Tests/CartTests.cs"]));

        // Act
        await session.DispatchAsync(new ExplorerCommand.ToggleFilter(ExplorerFilter.Updated));

        // Assert
        Assert.Equal(ExplorerFilter.Updated, session.State.ActiveFilter);
    }

    [Fact]
    public async Task It_brings_every_suite_back_when_the_same_filter_is_pressed_again()
    {
        // Arrange
        var session = await LoadedSessionAsync(new InMemoryUpdatedSource(["/repo/Shop.Tests/CartTests.cs"]));
        await session.DispatchAsync(new ExplorerCommand.ToggleFilter(ExplorerFilter.Updated));

        // Act
        await session.DispatchAsync(new ExplorerCommand.ToggleFilter(ExplorerFilter.Updated));

        // Assert
        Assert.Equal(
            ["Shop.Tests", "CartTests", "Adds item", "OrderTests", "Submits order"],
            session.State.VisibleNodes.Select(node => node.Name));
    }

    [Fact]
    public async Task It_runs_only_the_updated_suites()
    {
        // Arrange
        var backend = new InMemoryTestBackend(CartAndOrderTests());
        var session = new TestExplorerSession(
            backend,
            updatedSourceProvider: new InMemoryUpdatedSource(["/repo/Shop.Tests/CartTests.cs"]));
        await session.LoadAsync("/repo/Shop.sln");
        await session.DispatchAsync(new ExplorerCommand.ToggleFilter(ExplorerFilter.Updated));

        // Act
        await session.DispatchAsync(new ExplorerCommand.RunSelected());

        // Assert
        Assert.Equal(["Shop.Tests.CartTests.Adds_item"], backend.LastRun.Select(test => test.FullyQualifiedName));
    }

    [Fact]
    public async Task It_shows_nothing_when_no_source_changed()
    {
        // Arrange
        var session = await LoadedSessionAsync(new InMemoryUpdatedSource([]));

        // Act
        await session.DispatchAsync(new ExplorerCommand.ToggleFilter(ExplorerFilter.Updated));

        // Assert
        Assert.Empty(session.State.VisibleNodes);
    }

    [Fact]
    public async Task It_narrows_the_updated_suites_to_the_search()
    {
        // Arrange
        var session = await LoadedSessionAsync(
            new InMemoryUpdatedSource(["/repo/Shop.Tests/CartTests.cs", "/repo/Shop.Tests/OrderTests.cs"]));
        await session.DispatchAsync(new ExplorerCommand.ToggleFilter(ExplorerFilter.Updated));

        // Act
        await session.DispatchAsync(new ExplorerCommand.Search("submits"));

        // Assert
        Assert.Equal(
            ["Shop.Tests", "OrderTests", "Submits order"],
            session.State.VisibleNodes.Select(node => node.Name));
    }

    private static async Task<TestExplorerSession> LoadedSessionAsync(IUpdatedSourceProvider updatedSource)
    {
        var session = new TestExplorerSession(
            new InMemoryTestBackend(CartAndOrderTests()),
            updatedSourceProvider: updatedSource);
        await session.LoadAsync("/repo/Shop.sln");
        return session;
    }

    private static IReadOnlyList<TestCase> CartAndOrderTests() =>
    [
        new("Shop.Tests.CartTests.Adds_item", "Adds item", "/repo/Shop.Tests/Shop.Tests.csproj"),
        new("Shop.Tests.OrderTests.Submits_order", "Submits order", "/repo/Shop.Tests/Shop.Tests.csproj")
    ];

    private sealed class InMemoryUpdatedSource(IReadOnlyList<string> paths) : IUpdatedSourceProvider
    {
        public Task<IReadOnlyList<UpdatedSource>> UpdatedSourcesAsync(
            string target,
            CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<UpdatedSource>>(
            [.. paths.Select(path => new UpdatedSource(path, ChangeKind.Modified))]);
    }

    private sealed class InMemoryTestBackend(IReadOnlyList<TestCase> tests) : ITestBackend
    {
        public IReadOnlyCollection<TestCase> LastRun { get; private set; } = [];

        public Task<IReadOnlyList<TestCase>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default) => Task.FromResult(tests);

        public Task<TestRun> RunAsync(
            IReadOnlyCollection<TestCase> tests,
            CancellationToken cancellationToken = default)
        {
            LastRun = tests;
            return Task.FromResult(new TestRun(true, "Passed"));
        }
    }
}
