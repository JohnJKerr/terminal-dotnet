using TerminalDotnet.Changes;
using TerminalDotnet.Explorer;
using TerminalDotnet.Files;
using TerminalDotnet.Terminal;
using TerminalDotnet.Testing;
using Xunit;

namespace TerminalDotnet.Tests.Terminal;

public sealed class WhenAPanelHasNothingToShow
{
    [Fact]
    public void It_tells_every_panel_that_it_has_nothing_to_show()
    {
        // Arrange
        var query = "";

        // Act
        var messages = EmptyMessages(query);

        // Assert
        Assert.Equal(["No files to show", "No tests to show", "No changes to show"], messages);
    }

    [Fact]
    public void It_names_the_search_that_every_panel_could_not_match()
    {
        // Arrange
        var query = "odr";

        // Act
        var messages = EmptyMessages(query);

        // Assert
        Assert.Equal(
            ["No files match 'odr'", "No tests match 'odr'", "No changes match 'odr'"],
            messages);
    }

    [Fact]
    public void It_leaves_every_panel_without_an_empty_state_while_it_has_rows()
    {
        // Arrange
        var file = new FileEntry("App.csproj", "App", "Order.cs", FileGitStatus.Unchanged);
        var test = new TestCase("App.Tests.OrderTests.Adds", "Adds", "App.Tests.csproj");

        // Act
        var messages = new[]
        {
            FilePanelSnapshot.From(new FileExplorerState(
                [new VisibleFileNode(2, FileNodeKind.File, "Order.cs", [file])])).EmptyMessage,
            TestPanelSnapshot.From(
                new ExplorerState(
                    ExplorerStatus.Ready,
                    [new VisibleTestNode(2, TestNodeKind.Test, test.DisplayName, [test])],
                    0,
                    "Ready"),
                "App.slnx").EmptyMessage,
            ChangesetPanelSnapshot.From(new ChangesetState(
                [new ChangedFile("/repo/Order.cs", "Order.cs", ChangeKind.Modified)])).EmptyMessage
        };

        // Assert
        Assert.Equal(["", "", ""], messages);
    }

    private static IReadOnlyList<string> EmptyMessages(string query) =>
    [
        FilePanelSnapshot.From(new FileExplorerState([], SearchQuery: query)).EmptyMessage,
        TestPanelSnapshot.From(
            new ExplorerState(ExplorerStatus.Ready, [], 0, "Ready", SearchQuery: query),
            "App.slnx").EmptyMessage,
        ChangesetPanelSnapshot.From(new ChangesetState([], SearchQuery: query)).EmptyMessage
    ];
}
