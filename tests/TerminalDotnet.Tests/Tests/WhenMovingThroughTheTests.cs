using TerminalDotnet.Explorer;
using TerminalDotnet.Testing;
using TerminalDotnet.Tests.Builders;
using Xunit;

namespace TerminalDotnet.Tests.Tests;

public sealed class WhenMovingThroughTheTests
{
    [Fact]
    public async Task It_stops_at_the_end_of_the_visible_tree_when_moving_down()
    {
        // Arrange
        var session = await GivenA.TestExplorer()
            .WithTests(GivenA.TestCase("Shop.Tests.CartTests.Adds_item"))
            .LoadedAsync();

        // Act
        await session.DispatchAsync(new ExplorerCommand.MoveDown());
        await session.DispatchAsync(new ExplorerCommand.MoveDown());
        await session.DispatchAsync(new ExplorerCommand.MoveDown());

        // Assert
        Assert.Equal(2, session.State.SelectedIndex);
    }

    [Fact]
    public async Task It_selects_the_previous_visible_node_when_moving_up()
    {
        // Arrange
        var session = await GivenA.TestExplorer()
            .WithTests(GivenA.TestCase("Shop.Tests.CartTests.Adds_item"))
            .LoadedAsync();
        await session.DispatchAsync(new ExplorerCommand.MoveDown());
        await session.DispatchAsync(new ExplorerCommand.MoveDown());

        // Act
        await session.DispatchAsync(new ExplorerCommand.MoveUp());

        // Assert
        Assert.Equal(1, session.State.SelectedIndex);
    }

    [Fact]
    public async Task It_loads_the_selected_test_source_before_any_test_has_run()
    {
        // Arrange
        var test = GivenA.TestCase("Shop.Tests.CartTests.Adds_item");
        var source = new SourceLocation("CartTests.cs", 5);
        var session = await GivenA.TestExplorer()
            .WithTests(test)
            .WithSourceAt(source)
            .LoadedAsync();
        await session.DispatchAsync(new ExplorerCommand.MoveDown());
        await session.DispatchAsync(new ExplorerCommand.MoveDown());

        // Act
        await session.DispatchAsync(new ExplorerCommand.LoadSelectedSource());

        // Assert
        Assert.Same(source, session.State.SourceLocation);
    }
}
