using TerminalDotnet.Explorer;
using TerminalDotnet.Testing;
using TerminalDotnet.Tests.Builders;
using Xunit;

namespace TerminalDotnet.Tests.Testing;

public sealed class WhenDiscoveringTheTests
{
    [Fact]
    public async Task It_counts_every_test_it_discovers()
    {
        // Arrange
        var session = SessionWithCartTests();

        // Act
        await session.LoadAsync("/repo/Shop.sln");

        // Assert
        Assert.Equal(2, session.State.DiscoveredTestCount);
    }

    [Fact]
    public async Task It_produces_a_project_class_and_test_tree_when_loaded()
    {
        // Arrange
        var session = SessionWithCartTests();

        // Act
        await session.LoadAsync("/repo/Shop.sln");

        // Assert
        Assert.Equal(
        [
            "0:Project:Shop.Tests",
            "1:Class:CartTests",
            "2:Test:Adding item updates total",
            "2:Test:Empty cart has zero total"
        ],
            session.State.VisibleNodes.Select(node => $"{node.Depth}:{node.Kind}:{node.Name}"));
    }

    [Fact]
    public async Task It_is_ready_when_loaded()
    {
        // Arrange
        var session = SessionWithCartTests();

        // Act
        await session.LoadAsync("/repo/Shop.sln");

        // Assert
        Assert.Equal(ExplorerStatus.Ready, session.State.Status);
    }

    [Fact]
    public async Task It_selects_the_first_node_when_loaded()
    {
        // Arrange
        var session = SessionWithCartTests();

        // Act
        await session.LoadAsync("/repo/Shop.sln");

        // Assert
        Assert.Equal(0, session.State.SelectedIndex);
    }

    private static TestExplorerSession SessionWithCartTests() => GivenA.TestExplorer()
        .WithTests(
            GivenA.TestCase("Shop.Tests.CartTests.Adding_item_updates_total"),
            GivenA.TestCase("Shop.Tests.CartTests.Empty_cart_has_zero_total"))
        .Build();
}
