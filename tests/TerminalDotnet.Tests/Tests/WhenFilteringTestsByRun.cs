using TerminalDotnet.Explorer;
using TerminalDotnet.Filters;
using TerminalDotnet.Testing;
using TerminalDotnet.Tests.Builders;
using Xunit;

namespace TerminalDotnet.Tests.Tests;

public sealed class WhenFilteringTestsByRun
{
    [Fact]
    public async Task Failing_keeps_tests_whose_latest_outcome_failed()
    {
        // Arrange
        var session = await SessionAfterFullRunAsync();

        // Act
        await session.DispatchAsync(new ExplorerCommand.ToggleFilter(ExplorerFilter.Failing));

        // Assert
        Assert.Equal(["Shop.Tests", "CartTests", "Removes item"], NamesOf(session));
    }

    [Fact]
    public async Task Passing_keeps_tests_whose_latest_outcome_passed()
    {
        // Arrange
        var session = await SessionAfterFullRunAsync();

        // Act
        await session.DispatchAsync(new ExplorerCommand.ToggleFilter(ExplorerFilter.Passing));

        // Assert
        Assert.Equal(["Shop.Tests", "CartTests", "Adds item"], NamesOf(session));
    }

    [Fact]
    public async Task Last_run_keeps_tests_selected_for_the_most_recent_run()
    {
        // Arrange
        var session = await LoadedSessionAsync();
        await session.DispatchAsync(new ExplorerCommand.SelectIndex(2));
        await session.DispatchAsync(new ExplorerCommand.RunSelected());

        // Act
        await session.DispatchAsync(new ExplorerCommand.ToggleFilter(ExplorerFilter.LastRun));

        // Assert
        Assert.Equal(["Shop.Tests", "CartTests", "Adds item"], NamesOf(session));
    }

    [Fact]
    public async Task Not_run_keeps_tests_without_a_completed_outcome()
    {
        // Arrange
        var session = await LoadedSessionAsync();
        await session.DispatchAsync(new ExplorerCommand.SelectIndex(2));
        await session.DispatchAsync(new ExplorerCommand.RunSelected());

        // Act
        await session.DispatchAsync(new ExplorerCommand.ToggleFilter(ExplorerFilter.NotRun));

        // Assert
        Assert.Equal(
            ["Shop.Tests", "CartTests", "Removes item", "Skips item"],
            NamesOf(session));
    }

    [Fact]
    public async Task Pressing_the_active_run_filter_again_brings_every_test_back()
    {
        // Arrange
        var session = await SessionAfterFullRunAsync();
        await session.DispatchAsync(new ExplorerCommand.ToggleFilter(ExplorerFilter.Failing));

        // Act
        await session.DispatchAsync(new ExplorerCommand.ToggleFilter(ExplorerFilter.Failing));

        // Assert
        Assert.Equal(
            ["Shop.Tests", "CartTests", "Adds item", "Removes item", "Skips item"],
            NamesOf(session));
    }

    private static async Task<TestExplorerSession> SessionAfterFullRunAsync()
    {
        var session = await LoadedSessionAsync();
        await session.DispatchAsync(new ExplorerCommand.RunSelected());
        return session;
    }

    private static async Task<TestExplorerSession> LoadedSessionAsync()
    {
        var session = GivenA.TestExplorer()
            .WithBackend(new OutcomeBackend(Tests()))
            .Build();
        await session.LoadAsync(TestPaths.In("Shop.sln"));
        return session;
    }

    private static IReadOnlyList<string> NamesOf(TestExplorerSession session) =>
        session.State.VisibleNodes.Select(node => node.Name).ToArray();

    private static IReadOnlyList<TestCase> Tests() =>
    [
        new("Shop.Tests.CartTests.Adds_item", "Adds item", TestPaths.In("Shop.Tests", "Shop.Tests.csproj")),
        new("Shop.Tests.CartTests.Removes_item", "Removes item", TestPaths.In("Shop.Tests", "Shop.Tests.csproj")),
        new("Shop.Tests.CartTests.Skips_item", "Skips item", TestPaths.In("Shop.Tests", "Shop.Tests.csproj"))
    ];

    private sealed class OutcomeBackend(IReadOnlyList<TestCase> tests) : ITestBackend
    {
        public Task<IReadOnlyList<TestCase>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default) => Task.FromResult(tests);

        public Task<TestRun> RunAsync(
            IReadOnlyCollection<TestCase> selected,
            CancellationToken cancellationToken = default)
        {
            var results = selected.Select(ResultFor).ToArray();
            return Task.FromResult(new TestRun(results.All(result => result.Outcome != TestOutcome.Failed), "Done", results));
        }

        private static TestResult ResultFor(TestCase test) => new(
            test,
            test.DisplayName switch
            {
                "Removes item" => TestOutcome.Failed,
                "Skips item" => TestOutcome.Skipped,
                _ => TestOutcome.Passed
            },
            TimeSpan.Zero,
            null,
            null,
            null,
            null);
    }
}
