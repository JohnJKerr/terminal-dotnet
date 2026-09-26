using TerminalDotnet.Changes;
using TerminalDotnet.Explorer;
using TerminalDotnet.Testing;
using TerminalDotnet.Tests.Builders;
using Xunit;

namespace TerminalDotnet.Tests.Tests;

public sealed class WhenShowingWhichTestsChanged
{
    [Fact]
    public async Task It_marks_the_suite_of_an_added_file()
    {
        // Arrange
        var session = await LoadedSessionAsync(
            new UpdatedSource(TestPaths.In("Shop.Tests", "CartTests.cs"), ChangeKind.Added));

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
            new UpdatedSource(TestPaths.In("Shop.Tests", "CartTests.cs"), ChangeKind.Modified));

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
            new UpdatedSource(TestPaths.In("Shop.Tests", "CartTests.cs"), ChangeKind.Added));

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
            new UpdatedSource(TestPaths.In("Shop.Tests", "CartTests.cs"), ChangeKind.Added));

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
            new UpdatedSource(TestPaths.In("Shop.Tests", "CartTests.cs"), ChangeKind.Added));

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
            new UpdatedSource(TestPaths.In("Shop.Tests", "CartTests.cs"), ChangeKind.Added));

        // Act
        await session.DispatchAsync(new ExplorerCommand.RunSelected());

        // Assert
        Assert.Equal(TestNodeUpdate.Added, SuiteNamed(session, "CartTests").Update);
    }

    private static VisibleTestNode SuiteNamed(TestExplorerSession session, string name) =>
        session.State.VisibleNodes.Single(node =>
            node.Kind == TestNodeKind.Class && node.Name == name);

    private static Task<TestExplorerSession> LoadedSessionAsync(params UpdatedSource[] sources) => GivenA.TestExplorer()
        .WithTests(
            GivenA.TestCase("Shop.Tests.CartTests.Adds_item", TestPaths.In("Shop.Tests", "Shop.Tests.csproj")),
            GivenA.TestCase("Shop.Tests.OrderTests.Submits_order", TestPaths.In("Shop.Tests", "Shop.Tests.csproj")))
        .WithUpdatedSources(sources)
        .LoadedAsync(TestPaths.In("Shop.sln"));
}
