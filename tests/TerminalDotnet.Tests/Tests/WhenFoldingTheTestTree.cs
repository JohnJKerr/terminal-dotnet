using TerminalDotnet.Explorer;
using TerminalDotnet.Testing;
using TerminalDotnet.Tests.Builders;
using Xunit;

namespace TerminalDotnet.Tests.Tests;

public sealed class WhenFoldingTheTestTree
{
    [Fact]
    public async Task It_hides_a_classes_tests_when_collapsed()
    {
        // Arrange
        var session = await GivenA.TestExplorer()
            .WithTests(
                GivenA.TestCase("Shop.Tests.CartTests.Adds_item"),
                GivenA.TestCase("Shop.Tests.OrderTests.Submits_order"))
            .LoadedAsync();
        await session.DispatchAsync(new ExplorerCommand.MoveDown());

        // Act
        await session.DispatchAsync(new ExplorerCommand.ToggleExpanded());

        // Assert
        Assert.Equal(
        [
            "Project:Shop.Tests",
            "Class:CartTests",
            "Class:OrderTests",
            "Test:Submits order"
        ], session.State.VisibleNodes.Select(node => $"{node.Kind}:{node.Name}"));
    }

    [Fact]
    public async Task It_restores_a_classes_tests_when_expanded()
    {
        // Arrange
        var session = await GivenA.TestExplorer()
            .WithTests(GivenA.TestCase("Shop.Tests.CartTests.Adds_item"))
            .LoadedAsync();
        await session.DispatchAsync(new ExplorerCommand.MoveDown());
        await session.DispatchAsync(new ExplorerCommand.ToggleExpanded());

        // Act
        await session.DispatchAsync(new ExplorerCommand.ToggleExpanded());

        // Assert
        Assert.Equal(
        ["Project:Shop.Tests", "Class:CartTests", "Test:Adds item"],
            session.State.VisibleNodes.Select(node => $"{node.Kind}:{node.Name}"));
    }

    [Fact]
    public async Task It_folds_every_project_and_class_when_told_to_fold_everything()
    {
        // Arrange
        var session = await GivenA.TestExplorer()
            .WithTests(
                GivenA.TestCase("Shop.Tests.CartTests.Adds_item"),
                GivenA.TestCase("Shop.Tests.OrderTests.Submits_order"))
            .LoadedAsync();

        // Act
        await session.DispatchAsync(new ExplorerCommand.ToggleAllExpanded());

        // Assert
        Assert.Equal(
            ["Project:Shop.Tests"],
            session.State.VisibleNodes.Select(node => $"{node.Kind}:{node.Name}"));
    }

    [Fact]
    public async Task It_unfolds_every_project_and_class_when_everything_is_already_folded()
    {
        // Arrange
        var session = await GivenA.TestExplorer()
            .WithTests(
                GivenA.TestCase("Shop.Tests.CartTests.Adds_item"),
                GivenA.TestCase("Shop.Tests.OrderTests.Submits_order"))
            .LoadedAsync();
        await session.DispatchAsync(new ExplorerCommand.ToggleAllExpanded());

        // Act
        await session.DispatchAsync(new ExplorerCommand.ToggleAllExpanded());

        // Assert
        Assert.Equal(
        [
            "Project:Shop.Tests",
            "Class:CartTests",
            "Test:Adds item",
            "Class:OrderTests",
            "Test:Submits order"
        ], session.State.VisibleNodes.Select(node => $"{node.Kind}:{node.Name}"));
    }

    [Fact]
    public async Task It_folds_everything_while_part_of_the_tree_is_already_folded()
    {
        // Arrange
        var session = await GivenA.TestExplorer()
            .WithTests(
                GivenA.TestCase("Shop.Tests.CartTests.Adds_item"),
                GivenA.TestCase("Shop.Tests.OrderTests.Submits_order"))
            .LoadedAsync();
        await session.DispatchAsync(new ExplorerCommand.MoveDown());
        await session.DispatchAsync(new ExplorerCommand.ToggleExpanded());

        // Act
        await session.DispatchAsync(new ExplorerCommand.ToggleAllExpanded());

        // Assert
        Assert.Equal(
            ["Project:Shop.Tests"],
            session.State.VisibleNodes.Select(node => $"{node.Kind}:{node.Name}"));
    }

    [Fact]
    public async Task It_keeps_the_selection_on_a_visible_row_when_folding_everything()
    {
        // Arrange
        var session = await GivenA.TestExplorer()
            .WithTests(GivenA.TestCase("Shop.Tests.CartTests.Adds_item"))
            .LoadedAsync();
        await session.DispatchAsync(new ExplorerCommand.MoveDown());
        await session.DispatchAsync(new ExplorerCommand.MoveDown());

        // Act
        await session.DispatchAsync(new ExplorerCommand.ToggleAllExpanded());

        // Assert
        Assert.Equal("Shop.Tests", session.State.VisibleNodes[session.State.SelectedIndex].Name);
    }

    [Fact]
    public async Task It_restores_test_outcomes_when_a_class_is_expanded_after_a_run()
    {
        // Arrange
        var test = GivenA.TestCase("Shop.Tests.CartTests.Adds_item");
        var session = await GivenA.TestExplorer()
            .WithTests(test)
            .LoadedAsync();
        await session.DispatchAsync(new ExplorerCommand.MoveDown());
        await session.DispatchAsync(new ExplorerCommand.RunSelected());
        await session.DispatchAsync(new ExplorerCommand.ToggleExpanded());

        // Act
        await session.DispatchAsync(new ExplorerCommand.ToggleExpanded());

        // Assert
        Assert.Equal(TestNodeOutcome.Passed, session.State.VisibleNodes[2].Outcome);
    }
}
