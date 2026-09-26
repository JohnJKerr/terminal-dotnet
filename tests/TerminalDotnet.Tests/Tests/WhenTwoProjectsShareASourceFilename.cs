using TerminalDotnet.Explorer;
using TerminalDotnet.Testing;
using TerminalDotnet.Tests.Builders;
using Xunit;

namespace TerminalDotnet.Tests.Tests;

public sealed class WhenTwoProjectsShareASourceFilename
{
    [Fact]
    public async Task It_marks_the_suite_in_the_project_whose_file_changed()
    {
        // Arrange
        var session = await LoadedSessionAsync();

        // Act
        var suite = SuiteIn(session, TestPaths.In("Shop.Tests", "Shop.Tests.csproj"));

        // Assert
        Assert.Equal(TestNodeUpdate.Edited, suite.Update);
    }

    [Fact]
    public async Task It_leaves_the_suite_in_the_other_project_unmarked()
    {
        // Arrange
        var session = await LoadedSessionAsync();

        // Act
        var suite = SuiteIn(session, TestPaths.In("Admin.Tests", "Admin.Tests.csproj"));

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

    private static Task<TestExplorerSession> LoadedSessionAsync() => GivenA.TestExplorer()
        .WithTests(
            GivenA.TestCase("Shop.Tests.CartTests.Adds_item", TestPaths.In("Shop.Tests", "Shop.Tests.csproj")),
            GivenA.TestCase("Admin.Tests.CartTests.Adds_item", TestPaths.In("Admin.Tests", "Admin.Tests.csproj")))
        .WithEditedSources(TestPaths.In("Shop.Tests", "CartTests.cs"))
        .LoadedAsync(TestPaths.In("Shop.sln"));
}
