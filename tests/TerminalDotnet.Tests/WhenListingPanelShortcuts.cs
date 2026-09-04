using TerminalDotnet.Changes;
using TerminalDotnet.Explorer;
using TerminalDotnet.Files;
using TerminalDotnet.Terminal;
using TerminalDotnet.Testing;
using Xunit;

namespace TerminalDotnet.Tests.Terminal;

public sealed class WhenListingPanelShortcuts
{
    [Fact]
    public void It_only_offers_file_actions_for_a_selected_file()
    {
        // Arrange
        var file = new FileEntry("App.csproj", "App", "Program.cs", FileGitStatus.Unchanged);
        var fileState = new FileExplorerState(
            [new VisibleFileNode(2, FileNodeKind.File, "Program.cs", [file])]);

        // Act
        var shortcuts = PanelShortcuts.For(PanelKind.Explorer, fileState, EmptyChangeset(), EmptyTestState());

        // Assert
        Assert.Equal(
            "Tab pane  s search  ↑/k up  ↓/j down  Enter/e edit  p preview  q quit",
            shortcuts);
    }

    [Fact]
    public void It_offers_folding_for_a_selected_file_group()
    {
        // Arrange
        var file = new FileEntry("App.csproj", "App", "Program.cs", FileGitStatus.Unchanged);
        var fileState = new FileExplorerState(
            [new VisibleFileNode(0, FileNodeKind.Project, "App", [file])]);

        // Act
        var shortcuts = PanelShortcuts.For(PanelKind.Explorer, fileState, EmptyChangeset(), EmptyTestState());

        // Assert
        Assert.Equal(
            "Tab pane  s search  ↑/k up  ↓/j down  Space/Enter fold  q quit",
            shortcuts);
    }

    [Fact]
    public void It_hides_test_output_before_tests_have_run()
    {
        // Arrange
        var state = TestState();

        // Act
        var shortcuts = PanelShortcuts.For(PanelKind.Tests, new FileExplorerState([]), EmptyChangeset(), state);

        // Assert
        Assert.DoesNotContain("output", shortcuts);
    }

    [Fact]
    public void It_offers_test_output_after_tests_have_run()
    {
        // Arrange
        var state = TestState() with { LastRun = new TestRun(true, "Passed") };

        // Act
        var shortcuts = PanelShortcuts.For(PanelKind.Tests, new FileExplorerState([]), EmptyChangeset(), state);

        // Assert
        Assert.Contains("o output", shortcuts);
    }

    [Fact]
    public void It_only_offers_cancel_while_tests_are_running()
    {
        // Arrange
        var state = TestState() with { Status = ExplorerStatus.Running };

        // Act
        var shortcuts = PanelShortcuts.For(PanelKind.Tests, new FileExplorerState([]), EmptyChangeset(), state);

        // Assert
        Assert.Equal(1, shortcuts.Split("c cancel").Length - 1);
    }

    [Fact]
    public void It_offers_failure_actions_when_the_last_run_failed()
    {
        // Arrange
        var test = Test();
        var failed = new TestResult(test, TestOutcome.Failed, TimeSpan.Zero, "Failed", null, null, null);
        var state = TestState() with { LastRun = new TestRun(false, "Failed", [failed]) };

        // Act
        var shortcuts = PanelShortcuts.For(PanelKind.Tests, new FileExplorerState([]), EmptyChangeset(), state);

        // Assert
        Assert.Contains("F failures", shortcuts);
    }

    [Fact]
    public void It_offers_diff_edit_and_preview_for_a_changed_file()
    {
        // Arrange
        var changed = new ChangedFile("/repo/src/Order.cs", "Order.cs", ChangeKind.Modified);

        // Act
        var shortcuts = PanelShortcuts.For(
            PanelKind.Changes,
            new FileExplorerState([]),
            new ChangesetState([changed]),
            EmptyTestState());

        // Assert
        Assert.Equal(
            "Tab pane  s search  ↑/k up  ↓/j down  Enter/d diff  e edit  p preview  q quit",
            shortcuts);
    }

    [Fact]
    public void It_offers_restore_instead_of_editing_for_a_deleted_file()
    {
        // Arrange
        var deleted = new ChangedFile("/repo/src/Gone.cs", "Gone.cs", ChangeKind.Deleted);

        // Act
        var shortcuts = PanelShortcuts.For(
            PanelKind.Changes,
            new FileExplorerState([]),
            new ChangesetState([deleted]),
            EmptyTestState());

        // Assert
        Assert.Equal(
            "Tab pane  s search  ↑/k up  ↓/j down  Enter/d diff  r restore  q quit",
            shortcuts);
    }

    private static ChangesetState EmptyChangeset() => new([]);

    private static ExplorerState EmptyTestState() =>
        new(ExplorerStatus.Ready, [], 0, "Ready");

    private static ExplorerState TestState()
    {
        var test = Test();
        return new ExplorerState(
            ExplorerStatus.Ready,
            [new VisibleTestNode(2, TestNodeKind.Test, test.DisplayName, [test])],
            0,
            "Ready");
    }

    private static TestCase Test() =>
        new("App.Tests.ExampleTests.Passes", "Passes", "App.Tests.csproj");
}
