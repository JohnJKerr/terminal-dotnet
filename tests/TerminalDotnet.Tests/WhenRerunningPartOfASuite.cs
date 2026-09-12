using TerminalDotnet.Explorer;
using TerminalDotnet.Testing;
using Xunit;

namespace TerminalDotnet.Tests.Testing;

public sealed class WhenRerunningPartOfASuite
{
    [Fact]
    public async Task It_fails_the_class_holding_the_test_that_failed()
    {
        // Arrange
        var session = await SessionWithAFailedRerunAsync();

        // Act
        var classNode = session.State.VisibleNodes[1];

        // Assert
        Assert.Equal(TestNodeOutcome.Failed, classNode.Outcome);
    }

    [Fact]
    public async Task It_still_fails_that_class_once_the_tree_is_rebuilt()
    {
        // Arrange
        var session = await SessionWithAFailedRerunAsync();

        // Act
        await session.DispatchAsync(new ExplorerCommand.Search("item"));

        // Assert
        Assert.Equal(TestNodeOutcome.Failed, session.State.VisibleNodes[1].Outcome);
    }

    [Fact]
    public async Task It_keeps_the_test_that_passed_passing()
    {
        // Arrange
        var session = await SessionWithAFailedRerunAsync();

        // Act
        var removesItem = session.State.VisibleNodes[3];

        // Assert
        Assert.Equal(TestNodeOutcome.Passed, removesItem.Outcome);
    }

    private static readonly TestCase AddsItem =
        new("Shop.Tests.CartTests.Adds_item", "Adds item", "Shop.Tests.csproj");

    private static readonly TestCase RemovesItem =
        new("Shop.Tests.CartTests.Removes_item", "Removes item", "Shop.Tests.csproj");

    private static async Task<TestExplorerSession> SessionWithAFailedRerunAsync()
    {
        var backend = new QueuedRunBackend(
            [AddsItem, RemovesItem],
            new TestRun(true, "Passed", [Result(AddsItem, TestOutcome.Passed), Result(RemovesItem, TestOutcome.Passed)]),
            new TestRun(false, "Failed", [Result(AddsItem, TestOutcome.Failed)]));
        var session = new TestExplorerSession(backend);
        await session.LoadAsync("/repo/Shop.sln");
        await session.DispatchAsync(new ExplorerCommand.MoveDown());
        await session.DispatchAsync(new ExplorerCommand.RunSelected());
        await session.DispatchAsync(new ExplorerCommand.MoveDown());
        await session.DispatchAsync(new ExplorerCommand.RunSelected());
        return session;
    }

    private static TestResult Result(TestCase test, TestOutcome outcome) =>
        new(test, outcome, TimeSpan.Zero, null, null, null, null);

    private sealed class QueuedRunBackend(IReadOnlyList<TestCase> tests, params TestRun[] runs)
        : ITestBackend
    {
        private readonly Queue<TestRun> remaining = new(runs);

        public Task<IReadOnlyList<TestCase>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default) => Task.FromResult(tests);

        public Task<TestRun> RunAsync(
            IReadOnlyCollection<TestCase> requested,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(remaining.Dequeue());
    }
}
