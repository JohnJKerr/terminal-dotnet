using TerminalDotnet.Explorer;
using TerminalDotnet.Testing;
using Xunit;

namespace TerminalDotnet.Tests.Testing;

public sealed class WhenTwoSuitesShareAName
{
    [Fact]
    public async Task It_shows_a_class_of_its_own_for_each_namespace()
    {
        // Arrange
        var session = new TestExplorerSession(new InMemoryTestBackend([SalesSaving, InventorySaving]));

        // Act
        await session.LoadAsync("/repo/Shop.sln");

        // Assert
        Assert.Equal(
            2,
            session.State.VisibleNodes.Count(node => node.Kind == TestNodeKind.Class));
    }

    [Fact]
    public async Task It_runs_only_the_tests_of_the_class_that_is_selected()
    {
        // Arrange
        var backend = new InMemoryTestBackend([SalesSaving, InventorySaving]);
        var session = new TestExplorerSession(backend);
        await session.LoadAsync("/repo/Shop.sln");
        await session.DispatchAsync(new ExplorerCommand.MoveDown());

        // Act
        await session.DispatchAsync(new ExplorerCommand.RunSelected());

        // Assert
        Assert.Equal(
            ["Shop.Tests.Inventory.WhenSaving.It_saves"],
            backend.LastRun.Select(test => test.FullyQualifiedName));
    }

    private static readonly TestCase SalesSaving =
        new("Shop.Tests.Sales.WhenSaving.It_saves", "It saves", "Shop.Tests.csproj");

    private static readonly TestCase InventorySaving =
        new("Shop.Tests.Inventory.WhenSaving.It_saves", "It saves", "Shop.Tests.csproj");

    private sealed class InMemoryTestBackend(IReadOnlyList<TestCase> tests) : ITestBackend
    {
        public IReadOnlyCollection<TestCase> LastRun { get; private set; } = [];

        public Task<IReadOnlyList<TestCase>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default) => Task.FromResult(tests);

        public Task<TestRun> RunAsync(
            IReadOnlyCollection<TestCase> requested,
            CancellationToken cancellationToken = default)
        {
            LastRun = requested;
            return Task.FromResult(new TestRun(true, "Passed"));
        }
    }
}
