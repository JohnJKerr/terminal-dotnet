using TerminalDotnet.Explorer;
using TerminalDotnet.Filters;
using TerminalDotnet.Testing;
using TerminalDotnet.Tests.Builders;
using TerminalDotnet.Tests.Fakes;
using Xunit;

namespace TerminalDotnet.Tests.Testing;

public sealed class WhenFilteringToUpdatedTests
{
    [Fact]
    public async Task It_keeps_only_the_suites_whose_source_changed()
    {
        // Arrange
        var session = await LoadedSessionAsync(TestPaths.In("Shop.Tests", "CartTests.cs"));

        // Act
        await session.DispatchAsync(new ExplorerCommand.ToggleFilter(ExplorerFilter.Updated));

        // Assert
        Assert.Equal(
            ["Shop.Tests", "CartTests", "Adds item"],
            session.State.VisibleNodes.Select(node => node.Name));
    }

    [Fact]
    public async Task It_remembers_the_filter_it_is_using()
    {
        // Arrange
        var session = await LoadedSessionAsync(TestPaths.In("Shop.Tests", "CartTests.cs"));

        // Act
        await session.DispatchAsync(new ExplorerCommand.ToggleFilter(ExplorerFilter.Updated));

        // Assert
        Assert.Equal(ExplorerFilter.Updated, session.State.ActiveFilter);
    }

    [Fact]
    public async Task It_brings_every_suite_back_when_the_same_filter_is_pressed_again()
    {
        // Arrange
        var session = await LoadedSessionAsync(TestPaths.In("Shop.Tests", "CartTests.cs"));
        await session.DispatchAsync(new ExplorerCommand.ToggleFilter(ExplorerFilter.Updated));

        // Act
        await session.DispatchAsync(new ExplorerCommand.ToggleFilter(ExplorerFilter.Updated));

        // Assert
        Assert.Equal(
            ["Shop.Tests", "CartTests", "Adds item", "OrderTests", "Submits order"],
            session.State.VisibleNodes.Select(node => node.Name));
    }

    [Fact]
    public async Task It_runs_only_the_updated_suites()
    {
        // Arrange
        var backend = new InMemoryTestBackend(CartAndOrderTests());
        var session = await GivenA.TestExplorer()
            .WithBackend(backend)
            .WithEditedSources(TestPaths.In("Shop.Tests", "CartTests.cs"))
            .LoadedAsync(TestPaths.In("Shop.sln"));
        await session.DispatchAsync(new ExplorerCommand.ToggleFilter(ExplorerFilter.Updated));

        // Act
        await session.DispatchAsync(new ExplorerCommand.RunSelected());

        // Assert
        Assert.Equal(["Shop.Tests.CartTests.Adds_item"], backend.LastRun.Select(test => test.FullyQualifiedName));
    }

    [Fact]
    public async Task It_shows_nothing_when_no_source_changed()
    {
        // Arrange
        var session = await LoadedSessionAsync();

        // Act
        await session.DispatchAsync(new ExplorerCommand.ToggleFilter(ExplorerFilter.Updated));

        // Assert
        Assert.Empty(session.State.VisibleNodes);
    }

    [Fact]
    public async Task It_narrows_the_updated_suites_to_the_search()
    {
        // Arrange
        var session = await LoadedSessionAsync(
            TestPaths.In("Shop.Tests", "CartTests.cs"),
            TestPaths.In("Shop.Tests", "OrderTests.cs"));
        await session.DispatchAsync(new ExplorerCommand.ToggleFilter(ExplorerFilter.Updated));

        // Act
        await session.DispatchAsync(new ExplorerCommand.Search("submits"));

        // Assert
        Assert.Equal(
            ["Shop.Tests", "OrderTests", "Submits order"],
            session.State.VisibleNodes.Select(node => node.Name));
    }

    private static Task<TestExplorerSession> LoadedSessionAsync(params string[] editedSources) => GivenA.TestExplorer()
        .WithTests([.. CartAndOrderTests()])
        .WithEditedSources(editedSources)
        .LoadedAsync(TestPaths.In("Shop.sln"));

    private static IReadOnlyList<TestCase> CartAndOrderTests() =>
    [
        GivenA.TestCase("Shop.Tests.CartTests.Adds_item", TestPaths.In("Shop.Tests", "Shop.Tests.csproj")),
        GivenA.TestCase("Shop.Tests.OrderTests.Submits_order", TestPaths.In("Shop.Tests", "Shop.Tests.csproj"))
    ];
}
