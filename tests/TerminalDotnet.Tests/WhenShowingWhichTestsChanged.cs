using TerminalDotnet.Changes;
using TerminalDotnet.Explorer;
using TerminalDotnet.Testing;
using Xunit;

namespace TerminalDotnet.Tests.Testing;

public sealed class WhenShowingWhichTestsChanged
{
    [Fact]
    public async Task It_marks_the_suite_of_an_added_file()
    {
        // Arrange
        var session = await LoadedSessionAsync(
            new UpdatedSource("/repo/Shop.Tests/CartTests.cs", ChangeKind.Added));

        // Act
        var suite = SuiteNamed(session, "CartTests");

        // Assert
        Assert.Equal(TestNodeUpdate.Added, suite.Update);
    }

    [Fact]
    public async Task It_marks_the_suite_of_an_edited_file()
    {
        // Arrange
        var session = await LoadedSessionAsync(
            new UpdatedSource("/repo/Shop.Tests/CartTests.cs", ChangeKind.Modified));

        // Act
        var suite = SuiteNamed(session, "CartTests");

        // Assert
        Assert.Equal(TestNodeUpdate.Edited, suite.Update);
    }

    [Fact]
    public async Task It_marks_the_tests_of_a_changed_suite()
    {
        // Arrange
        var session = await LoadedSessionAsync(
            new UpdatedSource("/repo/Shop.Tests/CartTests.cs", ChangeKind.Added));

        // Act
        var test = session.State.VisibleNodes.Single(node => node.Name == "Adds item");

        // Assert
        Assert.Equal(TestNodeUpdate.Added, test.Update);
    }

    [Fact]
    public async Task It_leaves_an_untouched_suite_unmarked()
    {
        // Arrange
        var session = await LoadedSessionAsync(
            new UpdatedSource("/repo/Shop.Tests/CartTests.cs", ChangeKind.Added));

        // Act
        var suite = SuiteNamed(session, "OrderTests");

        // Assert
        Assert.Equal(TestNodeUpdate.Unchanged, suite.Update);
    }

    [Fact]
    public async Task It_leaves_the_project_row_unmarked()
    {
        // Arrange
        var session = await LoadedSessionAsync(
            new UpdatedSource("/repo/Shop.Tests/CartTests.cs", ChangeKind.Added));

        // Act
        var project = session.State.VisibleNodes.First(node => node.Kind == TestNodeKind.Project);

        // Assert
        Assert.Equal(TestNodeUpdate.Unchanged, project.Update);
    }

    [Fact]
    public async Task It_keeps_the_mark_through_a_run()
    {
        // Arrange
        var session = await LoadedSessionAsync(
            new UpdatedSource("/repo/Shop.Tests/CartTests.cs", ChangeKind.Added));

        // Act
        await session.DispatchAsync(new ExplorerCommand.RunSelected());

        // Assert
        Assert.Equal(TestNodeUpdate.Added, SuiteNamed(session, "CartTests").Update);
    }

    private static VisibleTestNode SuiteNamed(TestExplorerSession session, string name) =>
        session.State.VisibleNodes.Single(node =>
            node.Kind == TestNodeKind.Class && node.Name == name);

    private static async Task<TestExplorerSession> LoadedSessionAsync(params UpdatedSource[] sources)
    {
        var session = new TestExplorerSession(
            new InMemoryTestBackend(),
            updatedSourceProvider: new InMemoryUpdatedSource(sources));
        await session.LoadAsync("/repo/Shop.sln");
        return session;
    }

    private sealed class InMemoryUpdatedSource(IReadOnlyList<UpdatedSource> sources) : IUpdatedSourceProvider
    {
        public Task<IReadOnlyList<UpdatedSource>> UpdatedSourcesAsync(
            string target,
            CancellationToken cancellationToken = default) => Task.FromResult(sources);
    }

    private sealed class InMemoryTestBackend : ITestBackend
    {
        public Task<IReadOnlyList<TestCase>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TestCase>>(
            [
                new("Shop.Tests.CartTests.Adds_item", "Adds item", "/repo/Shop.Tests/Shop.Tests.csproj"),
                new("Shop.Tests.OrderTests.Submits_order", "Submits order", "/repo/Shop.Tests/Shop.Tests.csproj")
            ]);

        public Task<TestRun> RunAsync(
            IReadOnlyCollection<TestCase> tests,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new TestRun(true, "Passed"));
    }
}
