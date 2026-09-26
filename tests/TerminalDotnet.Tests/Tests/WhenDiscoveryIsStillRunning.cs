using TerminalDotnet.Explorer;
using TerminalDotnet.Testing;
using TerminalDotnet.Tests.Builders;
using Xunit;

namespace TerminalDotnet.Tests.Tests;

public sealed class WhenDiscoveryIsStillRunning
{
    private static readonly TestCase Adds =
        new("Shop.Tests.CartTests.Adds_item", "Adds item", "Shop.Tests.csproj");

    private static readonly TestCase Removes =
        new("Shop.Tests.CartTests.Removes_item", "Removes item", "Shop.Tests.csproj");

    [Fact]
    public async Task It_keeps_a_search_typed_before_the_tests_arrived()
    {
        // Arrange
        var backend = new PausedTestBackend([Adds, Removes]);
        var session = GivenA.TestExplorer()
            .WithBackend(backend)
            .Build();
        var load = session.LoadAsync("Shop.sln");
        await session.DispatchAsync(new ExplorerCommand.Search("Removes"));

        // Act
        backend.Release();
        await load;

        // Assert
        Assert.Equal("Removes", session.State.SearchQuery);
    }

    [Fact]
    public async Task It_shows_only_the_tests_matching_that_search()
    {
        // Arrange
        var backend = new PausedTestBackend([Adds, Removes]);
        var session = GivenA.TestExplorer()
            .WithBackend(backend)
            .Build();
        var load = session.LoadAsync("Shop.sln");
        await session.DispatchAsync(new ExplorerCommand.Search("Removes"));

        // Act
        backend.Release();
        await load;

        // Assert
        Assert.Equal(
            ["Removes item"],
            session.State.VisibleNodes
                .Where(node => node.Kind == TestNodeKind.Test)
                .Select(node => node.Name));
    }

    private sealed class PausedTestBackend(IReadOnlyList<TestCase> tests) : ITestBackend
    {
        private readonly TaskCompletionSource discovered =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Release() => discovered.SetResult();

        public async Task<IReadOnlyList<TestCase>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default)
        {
            await discovered.Task;
            return tests;
        }

        public Task<TestRun> RunAsync(
            IReadOnlyCollection<TestCase> tests,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
