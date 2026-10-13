using TerminalDotnet.Explorer;
using TerminalDotnet.Testing;
using TerminalDotnet.Tests.Builders;
using TerminalDotnet.Tests.Fakes;
using Xunit;

namespace TerminalDotnet.Tests.Tests;

public sealed class WhenRunningAllTests
{
    private static readonly TestCase Cart =
        new("Shop.CartTests.Adds_item", "Adds item", "/repo/Cart.Tests.csproj");

    private static readonly TestCase Orders =
        new("Shop.OrderTests.Submits_order", "Submits order", "/repo/Orders.Tests.csproj");

    [Fact]
    public async Task It_shows_a_root_above_the_projects()
    {
        // Arrange
        var session = await GivenA.TestExplorer().WithTests(Cart, Orders).LoadedAsync();

        // Act
        var first = session.State.VisibleNodes[0];

        // Assert
        Assert.Equal((TestNodeKind.Root, "All tests", 0), (first.Kind, first.Name, first.Depth));
    }

    [Fact]
    public async Task Running_the_root_selects_tests_from_every_project()
    {
        // Arrange
        var backend = new InMemoryTestBackend([Cart, Orders]);
        var session = await GivenA.TestExplorer().WithBackend(backend).LoadedAsync();

        // Act
        await session.DispatchAsync(new ExplorerCommand.RunSelected());

        // Assert
        Assert.Equal([Cart, Orders], backend.LastRun);
    }

    [Fact]
    public async Task It_nests_each_project_below_the_root()
    {
        // Arrange
        var session = await GivenA.TestExplorer().WithTests(Cart, Orders).LoadedAsync();

        // Act
        var projects = session.State.VisibleNodes.Where(node => node.Kind == TestNodeKind.Project);

        // Assert
        Assert.Equal(["1:Cart.Tests", "1:Orders.Tests"], projects.Select(node => $"{node.Depth}:{node.Name}"));
    }

    [Fact]
    public async Task Folding_the_root_hides_its_projects()
    {
        // Arrange
        var session = await GivenA.TestExplorer().WithTests(Cart, Orders).LoadedAsync();

        // Act
        await session.DispatchAsync(new ExplorerCommand.ToggleExpanded());

        // Assert
        Assert.Equal(["All tests"], session.State.VisibleNodes.Select(node => node.Name));
    }

    [Fact]
    public async Task The_root_shows_a_failure_in_any_project()
    {
        // Arrange
        var run = new TestRun(false, "Failed",
        [
            GivenA.ResultFor(Cart).Build(),
            GivenA.ResultFor(Orders).Failed().Build()
        ]);
        var session = await GivenA.TestExplorer()
            .WithBackend(new InMemoryTestBackend([Cart, Orders], run))
            .LoadedAsync();

        // Act
        await session.DispatchAsync(new ExplorerCommand.RunSelected());

        // Assert
        Assert.Equal(TestNodeOutcome.Failed, session.State.VisibleNodes[0].Outcome);
    }
}
