using TerminalDotnet.Changes;
using TerminalDotnet.Explorer;
using TerminalDotnet.Files;
using TerminalDotnet.Filters;
using TerminalDotnet.Terminal;
using TerminalDotnet.Testing;
using Xunit;

namespace TerminalDotnet.Tests.Terminal;

public sealed class WhenAPanelHasNothingToShow
{
    [Fact]
    public void The_explorer_has_no_files_to_show()
    {
        // Arrange
        var state = new FileExplorerState([]);

        // Act
        var snapshot = FilePanelSnapshot.From(state);

        // Assert
        Assert.Equal("No files to show", snapshot.EmptyMessage);
    }

    [Fact]
    public void The_test_panel_has_no_tests_to_show()
    {
        // Arrange
        var state = new ExplorerState(ExplorerStatus.Ready, [], 0, "Ready");

        // Act
        var snapshot = TestPanelSnapshot.From(state, "App.slnx");

        // Assert
        Assert.Equal("No tests to show", snapshot.EmptyMessage);
    }

    [Fact]
    public void The_changeset_has_no_changes_to_show()
    {
        // Arrange
        var state = new ChangesetState([]);

        // Act
        var snapshot = ChangesetPanelSnapshot.From(state);

        // Assert
        Assert.Equal("No changes to show", snapshot.EmptyMessage);
    }

    [Fact]
    public void The_explorer_names_the_search_no_file_matched()
    {
        // Arrange
        var state = new FileExplorerState([], SearchQuery: "odr");

        // Act
        var snapshot = FilePanelSnapshot.From(state);

        // Assert
        Assert.Equal("No files match 'odr'", snapshot.EmptyMessage);
    }

    [Fact]
    public void The_test_panel_names_the_search_no_test_matched()
    {
        // Arrange
        var state = new ExplorerState(ExplorerStatus.Ready, [], 0, "Ready", SearchQuery: "odr");

        // Act
        var snapshot = TestPanelSnapshot.From(state, "App.slnx");

        // Assert
        Assert.Equal("No tests match 'odr'", snapshot.EmptyMessage);
    }

    [Fact]
    public void The_changeset_names_the_search_no_change_matched()
    {
        // Arrange
        var state = new ChangesetState([], SearchQuery: "odr");

        // Act
        var snapshot = ChangesetPanelSnapshot.From(state);

        // Assert
        Assert.Equal("No changes match 'odr'", snapshot.EmptyMessage);
    }

    [Fact]
    public void The_explorer_has_no_updated_files_to_show()
    {
        // Arrange
        var state = new FileExplorerState([], ActiveFilter: ExplorerFilter.Updated);

        // Act
        var snapshot = FilePanelSnapshot.From(state);

        // Assert
        Assert.Equal("No updated files to show", snapshot.EmptyMessage);
    }

    [Fact]
    public void The_test_panel_has_no_updated_tests_to_show()
    {
        // Arrange
        var state = new ExplorerState(
            ExplorerStatus.Ready,
            [],
            0,
            "Ready",
            ActiveFilter: ExplorerFilter.Updated);

        // Act
        var snapshot = TestPanelSnapshot.From(state, "App.slnx");

        // Assert
        Assert.Equal("No updated tests to show", snapshot.EmptyMessage);
    }

    [Fact]
    public void The_explorer_names_the_search_no_updated_file_matched()
    {
        // Arrange
        var state = new FileExplorerState([], SearchQuery: "odr", ActiveFilter: ExplorerFilter.Updated);

        // Act
        var snapshot = FilePanelSnapshot.From(state);

        // Assert
        Assert.Equal("No updated files match 'odr'", snapshot.EmptyMessage);
    }

    [Fact]
    public void The_explorer_stays_silent_while_it_has_files()
    {
        // Arrange
        var file = new FileEntry("App.csproj", "App", "Order.cs", FileGitStatus.Unchanged);
        var state = new FileExplorerState([new VisibleFileNode(2, FileNodeKind.File, "Order.cs", [file])]);

        // Act
        var snapshot = FilePanelSnapshot.From(state);

        // Assert
        Assert.Equal("", snapshot.EmptyMessage);
    }

    [Fact]
    public void The_test_panel_stays_silent_while_it_has_tests()
    {
        // Arrange
        var test = new TestCase("App.Tests.OrderTests.Adds", "Adds", "App.Tests.csproj");
        var state = new ExplorerState(
            ExplorerStatus.Ready,
            [new VisibleTestNode(2, TestNodeKind.Test, test.DisplayName, [test])],
            0,
            "Ready");

        // Act
        var snapshot = TestPanelSnapshot.From(state, "App.slnx");

        // Assert
        Assert.Equal("", snapshot.EmptyMessage);
    }

    [Fact]
    public void The_changeset_stays_silent_while_it_has_changes()
    {
        // Arrange
        var state = new ChangesetState([new ChangedFile("/repo/Order.cs", "Order.cs", ChangeKind.Modified)]);

        // Act
        var snapshot = ChangesetPanelSnapshot.From(state);

        // Assert
        Assert.Equal("", snapshot.EmptyMessage);
    }
}
