using TerminalDotnet.Changes;
using TerminalDotnet.Explorer;
using TerminalDotnet.Files;
using TerminalDotnet.Issues;
using TerminalDotnet.Terminal;
using TerminalDotnet.Testing;
using TerminalDotnet.Tests.Builders;
using TerminalDotnet.Tests.Fakes;
using Xunit;

namespace TerminalDotnet.Tests.Workspace;

public sealed class WhenTheFirstBuildFails
{
    private static readonly CompilationIssue Broken =
        new("/repo/Cart.cs", "Cart.cs", 3, 1, "CS1002", "; expected", IssueSeverity.Error);

    private static readonly TestCase Adds =
        new("Shop.Tests.CartTests.Adds_item", "Adds item", "Shop.Tests.csproj");

    [Fact]
    public async Task It_lists_no_tests_from_the_last_build()
    {
        // Arrange
        var (startup, tests) = StartingOnABrokenBuild();

        // Act
        await startup.LoadPendingAsync(() => Task.CompletedTask);

        // Assert
        Assert.Equal(0, tests.State.DiscoveredTestCount);
    }

    [Fact]
    public async Task It_says_the_tests_wait_for_the_build()
    {
        // Arrange
        var (startup, tests) = StartingOnABrokenBuild();

        // Act
        await startup.LoadPendingAsync(() => Task.CompletedTask);

        // Assert
        Assert.Equal(TestExplorerSession.WaitingForBuildMessage, tests.State.Message);
    }

    private static (PanelStartup Startup, TestExplorerSession Tests) StartingOnABrokenBuild()
    {
        var tests = GivenA.TestExplorer().WithTests(Adds).Build();
        var startup = new PanelStartup(
            new FileExplorerSession(new FixedFiles()),
            new FileExplorerSession(new FixedFiles(), FileGrouping.Folder),
            new ChangesetSession(new InMemoryChangesetBackend()),
            tests,
            "/repo/Shop.sln",
            GivenA.IssuePanel().WithIssues(Broken).Build());
        return (startup, tests);
    }
}
