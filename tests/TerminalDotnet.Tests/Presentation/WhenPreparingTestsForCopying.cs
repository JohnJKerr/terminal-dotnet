using TerminalDotnet.Explorer;
using TerminalDotnet.Filters;
using TerminalDotnet.Terminal;
using TerminalDotnet.Testing;
using TerminalDotnet.Tests.Builders;
using TerminalDotnet.Tests.Fakes;
using Xunit;

namespace TerminalDotnet.Tests.Presentation;

public sealed class WhenPreparingTestsForCopying
{
    [Fact]
    public async Task It_names_the_selected_test_with_its_project()
    {
        // Arrange
        var test = GivenA.TestCase("Shop.Tests.CartTests.Adds_item", "/repo/Shop.Tests.csproj");
        var session = await GivenA.TestExplorer().WithTests(test).LoadedAsync();
        await session.DispatchAsync(new ExplorerCommand.MoveDown());
        await session.DispatchAsync(new ExplorerCommand.MoveDown());

        // Act
        var text = TestSelectionText.From(session.State);

        // Assert
        Assert.Equal("/repo/Shop.Tests.csproj\n  Shop.Tests.CartTests.Adds_item", text);
    }

    [Fact]
    public async Task It_copies_every_test_under_a_folded_group()
    {
        // Arrange
        var first = GivenA.TestCase("Shop.Tests.CartTests.Adds_item", "/repo/Shop.Tests.csproj");
        var second = GivenA.TestCase("Shop.Tests.CartTests.Removes_item", "/repo/Shop.Tests.csproj");
        var session = await GivenA.TestExplorer().WithTests(second, first).LoadedAsync();
        await session.DispatchAsync(new ExplorerCommand.ToggleExpanded());

        // Act
        var text = TestSelectionText.From(session.State);

        // Assert
        Assert.Equal(
            "/repo/Shop.Tests.csproj\n  Shop.Tests.CartTests.Adds_item\n  Shop.Tests.CartTests.Removes_item",
            text);
    }

    [Fact]
    public async Task It_copies_each_project_under_the_all_tests_root()
    {
        // Arrange
        var cart = GivenA.TestCase("Shop.CartTests.Adds_item", "/repo/Cart.Tests.csproj");
        var orders = GivenA.TestCase("Shop.OrderTests.Submits_order", "/repo/Orders.Tests.csproj");
        var session = await GivenA.TestExplorer().WithTests(orders, cart).LoadedAsync();

        // Act
        var text = TestSelectionText.From(session.State);

        // Assert
        Assert.Equal(
            "/repo/Cart.Tests.csproj\n  Shop.CartTests.Adds_item\n" +
            "/repo/Orders.Tests.csproj\n  Shop.OrderTests.Submits_order",
            text);
    }

    [Fact]
    public async Task The_failed_filter_copies_only_failed_tests()
    {
        // Arrange
        var passed = GivenA.TestCase("Shop.Tests.CartTests.Adds_item", "/repo/Shop.Tests.csproj");
        var failed = GivenA.TestCase("Shop.Tests.CartTests.Removes_item", "/repo/Shop.Tests.csproj");
        var run = new TestRun(false, "Failed",
        [
            GivenA.ResultFor(passed).Build(),
            GivenA.ResultFor(failed).Failed().Build()
        ]);
        var session = await GivenA.TestExplorer()
            .WithBackend(new InMemoryTestBackend([passed, failed], run))
            .LoadedAsync();
        await session.DispatchAsync(new ExplorerCommand.RunSelected());
        await session.DispatchAsync(new ExplorerCommand.ToggleFilter(ExplorerFilter.Failing));

        // Act
        var text = TestSelectionText.From(session.State);

        // Assert
        Assert.Equal("/repo/Shop.Tests.csproj\n  Shop.Tests.CartTests.Removes_item", text);
    }

    [Fact]
    public void It_has_nothing_to_copy_when_the_tree_is_empty()
    {
        // Arrange
        var state = new ExplorerState(ExplorerStatus.Ready, [], 0, "Ready");

        // Act
        var text = TestSelectionText.From(state);

        // Assert
        Assert.Equal("", text);
    }
}
