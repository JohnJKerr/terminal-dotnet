using TerminalDotnet.Explorer;
using TerminalDotnet.Testing;
using TerminalDotnet.Tests.Builders;
using Xunit;

namespace TerminalDotnet.Tests.Testing;

public sealed class WhenSearchingTheTests
{
    [Fact]
    public async Task It_keeps_case_insensitive_search_matches_with_their_ancestors()
    {
        // Arrange
        var session = await GivenA.TestExplorer()
            .WithTests(
                GivenA.TestCase("Shop.Tests.CartTests.Adds_item"),
                GivenA.TestCase("Shop.Tests.OrderTests.Submits_order"))
            .LoadedAsync();

        // Act
        await session.DispatchAsync(new ExplorerCommand.Search("ORDER"));

        // Assert
        Assert.Equal(
        [
            "node:0:Project:Shop.Tests",
            "node:1:Class:OrderTests",
            "node:2:Test:Submits order"
        ], session.State.VisibleNodes.Select(node => $"node:{node.Depth}:{node.Kind}:{node.Name}"));
    }

    [Fact]
    public async Task It_restores_the_test_tree_when_search_is_cleared()
    {
        // Arrange
        var session = await SessionSearchingForNothingAsync();

        // Act
        await session.DispatchAsync(new ExplorerCommand.ClearSearch());

        // Assert
        Assert.Equal(5, session.State.VisibleNodes.Count);
    }

    [Fact]
    public async Task It_selects_the_first_node_when_search_is_cleared()
    {
        // Arrange
        var session = await SessionSearchingForNothingAsync();

        // Act
        await session.DispatchAsync(new ExplorerCommand.ClearSearch());

        // Assert
        Assert.Equal(0, session.State.SelectedIndex);
    }

    [Fact]
    public async Task It_exposes_the_active_search_query()
    {
        // Arrange
        var session = await GivenA.TestExplorer()
            .WithTests(GivenA.TestCase("Shop.Tests.CartTests.Adds_item"))
            .LoadedAsync();

        // Act
        await session.DispatchAsync(new ExplorerCommand.Search("cart"));

        // Assert
        Assert.Equal("cart", session.State.SearchQuery);
    }

    [Fact]
    public async Task It_matches_the_displayed_test_name_when_searching()
    {
        // Arrange
        var session = await GivenA.TestExplorer()
            .WithTests(new TestCase("Shop.Tests.CartTests.Empty_cart", "Empty cart has zero total", "Shop.Tests.csproj"))
            .LoadedAsync();

        // Act
        await session.DispatchAsync(new ExplorerCommand.Search("zero total"));

        // Assert
        Assert.Equal("Empty cart has zero total", session.State.VisibleNodes[2].Name);
    }

    [Fact]
    public async Task It_matches_a_run_of_characters_anywhere_in_the_test_name()
    {
        // Arrange
        var session = await GivenA.TestExplorer()
            .WithTests(GivenA.TestCase("Shop.Tests.RefundPolicyTests.Rejects_after_window"))
            .LoadedAsync();

        // Act
        await session.DispatchAsync(new ExplorerCommand.Search("refundpolicy"));

        // Assert
        Assert.Equal("Rejects after window", session.State.VisibleNodes[2].Name);
    }

    [Fact]
    public async Task It_ignores_tests_that_only_scatter_the_query_across_their_name()
    {
        // Arrange
        var session = await GivenA.TestExplorer()
            .WithTests(GivenA.TestCase("Shop.Tests.RefundPolicyTests.Rejects_after_window"))
            .LoadedAsync();

        // Act
        await session.DispatchAsync(new ExplorerCommand.Search("rfpt"));

        // Assert
        Assert.Empty(session.State.VisibleNodes);
    }

    private static async Task<TestExplorerSession> SessionSearchingForNothingAsync()
    {
        var session = await GivenA.TestExplorer()
            .WithTests(
                GivenA.TestCase("Shop.Tests.CartTests.Adds_item"),
                GivenA.TestCase("Shop.Tests.OrderTests.Submits_order"))
            .LoadedAsync();
        await session.DispatchAsync(new ExplorerCommand.Search("missing"));
        return session;
    }
}
