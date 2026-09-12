using TerminalDotnet.Changes;
using TerminalDotnet.Explorer;
using TerminalDotnet.Testing;
using Xunit;

namespace TerminalDotnet.Tests.Testing;

public sealed class WhenTwoProjectsShareASourceFilename
{
    [Fact]
    public async Task It_marks_the_suite_in_the_project_whose_file_changed()
    {
        // Arrange
        var session = await LoadedSessionAsync();

        // Act
        var suite = SuiteIn(session, "/repo/Shop.Tests/Shop.Tests.csproj");

        // Assert
        Assert.Equal(TestNodeUpdate.Edited, suite.Update);
    }

    [Fact]
    public async Task It_leaves_the_suite_in_the_other_project_unmarked()
    {
        // Arrange
        var session = await LoadedSessionAsync();

        // Act
        var suite = SuiteIn(session, "/repo/Admin.Tests/Admin.Tests.csproj");

        // Assert
        Assert.Equal(TestNodeUpdate.Unchanged, suite.Update);
    }

    [Fact]
    public async Task It_filters_to_the_tests_of_the_project_whose_file_changed()
    {
        // Arrange
        var session = await LoadedSessionAsync();

        // Act
        await session.DispatchAsync(new ExplorerCommand.ToggleFilter(Filters.ExplorerFilter.Updated));

        // Assert
        Assert.Equal(
            ["Shop.Tests.CartTests.Adds_item"],
            session.State.VisibleNodes
                .Where(node => node.Kind == TestNodeKind.Test)
                .Select(node => node.Tests[0].FullyQualifiedName));
    }

    private static VisibleTestNode SuiteIn(TestExplorerSession session, string projectPath) =>
        session.State.VisibleNodes.Single(node =>
            node.Kind == TestNodeKind.Class && node.Tests[0].ProjectPath == projectPath);

    private static async Task<TestExplorerSession> LoadedSessionAsync()
    {
        var session = new TestExplorerSession(
            new InMemoryTestBackend(),
            updatedSourceProvider: new InMemoryUpdatedSource(
                new UpdatedSource("/repo/Shop.Tests/CartTests.cs", ChangeKind.Modified)));
        await session.LoadAsync("/repo/Shop.sln");
        return session;
    }

    private sealed class InMemoryUpdatedSource(params UpdatedSource[] sources) : IUpdatedSourceProvider
    {
        public Task<IReadOnlyList<UpdatedSource>> UpdatedSourcesAsync(
            string target,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<UpdatedSource>>(sources);
    }

    private sealed class InMemoryTestBackend : ITestBackend
    {
        public Task<IReadOnlyList<TestCase>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TestCase>>(
            [
                new("Shop.Tests.CartTests.Adds_item", "Adds item", "/repo/Shop.Tests/Shop.Tests.csproj"),
                new("Admin.Tests.CartTests.Adds_item", "Adds item", "/repo/Admin.Tests/Admin.Tests.csproj")
            ]);

        public Task<TestRun> RunAsync(
            IReadOnlyCollection<TestCase> tests,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new TestRun(true, "Passed"));
    }
}
