using TerminalDotnet.Explorer;
using TerminalDotnet.Testing;
using TerminalDotnet.Tests.Builders;
using TerminalDotnet.Tests.Fakes;
using Xunit;

namespace TerminalDotnet.Tests.Tests;

public sealed class WhenTwoSuitesShareAName
{
    [Fact]
    public async Task It_shows_a_class_of_its_own_for_each_namespace()
    {
        // Arrange
        var session = GivenA.TestExplorer().WithTests(SalesSaving, InventorySaving).Build();

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
        var session = await GivenA.TestExplorer()
            .WithBackend(backend)
            .LoadedAsync();
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
}
