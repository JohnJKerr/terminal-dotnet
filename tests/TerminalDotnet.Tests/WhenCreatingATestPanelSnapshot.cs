using TerminalDotnet.Explorer;
using TerminalDotnet.Terminal;
using TerminalDotnet.Testing;
using Xunit;

namespace TerminalDotnet.Tests.Testing;

public class WhenCreatingATestPanelSnapshot
{
    [Fact]
    public void It_preserves_the_explorer_selection()
    {
        // Arrange
        var state = new ExplorerState(
            ExplorerStatus.Ready,
            [],
            4,
            "Ready");

        // Act
        var snapshot = TestPanelSnapshot.From(state, "Example.slnx");

        // Assert
        Assert.Equal(4, snapshot.SelectedIndex);
    }

    [Fact]
    public void It_exposes_the_active_test_search()
    {
        // Arrange
        var state = new ExplorerState(
            ExplorerStatus.Ready,
            [],
            0,
            "Ready",
            SearchQuery: "cart");

        // Act
        var snapshot = TestPanelSnapshot.From(state, "Example.slnx");

        // Assert
        Assert.Equal("cart", snapshot.SearchQuery);
    }

    [Fact]
    public void It_counts_matching_tests_without_counting_ancestor_rows()
    {
        // Arrange
        var first = new TestCase("Shop.Tests.CartTests.Adds_item", "Adds item", "Shop.Tests.csproj");
        var second = new TestCase("Shop.Tests.CartTests.Removes_item", "Removes item", "Shop.Tests.csproj");
        var state = new ExplorerState(
            ExplorerStatus.Ready,
        [
            new VisibleTestNode(0, TestNodeKind.Project, "Shop.Tests", [first, second]),
            new VisibleTestNode(1, TestNodeKind.Class, "CartTests", [first, second]),
            new VisibleTestNode(2, TestNodeKind.Test, first.DisplayName, [first]),
            new VisibleTestNode(2, TestNodeKind.Test, second.DisplayName, [second])
        ],
            0,
            "Ready",
            SearchQuery: "item");

        // Act
        var snapshot = TestPanelSnapshot.From(state, "Example.slnx");

        // Assert
        Assert.Equal(2, snapshot.SearchHitCount);
    }

    [Fact]
    public void It_shows_a_completed_tests_duration()
    {
        // Arrange
        var test = new TestCase("Shop.Tests.CartTests.Adds_item", "Adds item", "Shop.Tests.csproj");
        var result = new TestResult(
            test,
            TestOutcome.Passed,
            TimeSpan.FromMilliseconds(7),
            null,
            null,
            null,
            null);
        var state = new ExplorerState(
            ExplorerStatus.Ready,
            [new VisibleTestNode(2, TestNodeKind.Test, test.DisplayName, [test], TestNodeOutcome.Passed)],
            0,
            "Passed",
            new TestRun(true, "Passed", [result]));

        // Act
        var snapshot = TestPanelSnapshot.From(state, "Example.slnx");

        // Assert
        Assert.Equal("    ✓ Adds item 7ms", snapshot.TestRows.Single());
    }

    [Fact]
    public void It_shows_the_test_count_for_a_test_group()
    {
        // Arrange
        var tests = new[]
        {
            new TestCase("Shop.Tests.CartTests.Adds_item", "Adds item", "Shop.Tests.csproj"),
            new TestCase("Shop.Tests.CartTests.Removes_item", "Removes item", "Shop.Tests.csproj")
        };
        var state = new ExplorerState(
            ExplorerStatus.Ready,
            [new VisibleTestNode(1, TestNodeKind.Class, "CartTests", tests)],
            0,
            "Ready");

        // Act
        var snapshot = TestPanelSnapshot.From(state, "Example.slnx");

        // Assert
        Assert.Equal("  ▼ CartTests 2", snapshot.TestRows.Single());
    }

    [Fact]
    public void It_exposes_a_breadcrumb_for_the_selected_test()
    {
        // Arrange
        var test = new TestCase("Shop.Tests.CartTests.Adds_item", "Adds item", "Shop.Tests.csproj");
        var state = new ExplorerState(
            ExplorerStatus.Ready,
            [new VisibleTestNode(2, TestNodeKind.Test, test.DisplayName, [test])],
            0,
            "Ready");

        // Act
        var snapshot = TestPanelSnapshot.From(state, "/repo/Shop.slnx");

        // Assert
        Assert.Equal("Shop.Tests › CartTests › Adds item", snapshot.Breadcrumb);
    }

    [Fact]
    public void It_summarizes_completed_test_outcomes()
    {
        // Arrange
        var passed = new TestCase("Shop.Tests.CartTests.Passes", "Passes", "Shop.Tests.csproj");
        var failed = new TestCase("Shop.Tests.CartTests.Fails", "Fails", "Shop.Tests.csproj");
        var skipped = new TestCase("Shop.Tests.CartTests.Skips", "Skips", "Shop.Tests.csproj");
        var run = new TestRun(false, "Finished",
        [
            new TestResult(passed, TestOutcome.Passed, TimeSpan.Zero, null, null, null, null),
            new TestResult(failed, TestOutcome.Failed, TimeSpan.Zero, "Failed", null, null, null),
            new TestResult(skipped, TestOutcome.Skipped, TimeSpan.Zero, null, null, null, null)
        ]);
        var state = new ExplorerState(
            ExplorerStatus.Failed,
            [new VisibleTestNode(0, TestNodeKind.Project, "Shop.Tests", [passed, failed, skipped])],
            0,
            "Finished",
            run);

        // Act
        var snapshot = TestPanelSnapshot.From(state, "Shop.slnx");

        // Assert
        Assert.Equal("1 Failed, 1 Passed, 1 Skipped", snapshot.StatusLine);
    }

    [Fact]
    public void It_shows_test_discovery_progress_before_a_run()
    {
        // Arrange
        var state = new ExplorerState(
            ExplorerStatus.Ready,
            [],
            0,
            "Ready — 12 tests discovered");

        // Act
        var snapshot = TestPanelSnapshot.From(state, "Shop.slnx");

        // Assert
        Assert.Equal("Ready — 12 tests discovered", snapshot.StatusLine);
    }

    [Fact]
    public void It_exposes_the_selected_tests_captured_output()
    {
        // Arrange
        var test = new TestCase("Shop.Tests.CartTests.Adds_item", "Adds item", "Shop.Tests.csproj");
        var result = new TestResult(
            test,
            TestOutcome.Passed,
            TimeSpan.Zero,
            null,
            null,
            null,
            null,
            "Cart total: 10");
        var state = new ExplorerState(
            ExplorerStatus.Ready,
            [new VisibleTestNode(2, TestNodeKind.Test, test.DisplayName, [test])],
            0,
            "Passed",
            new TestRun(true, "1 test passed", [result]));

        // Act
        var snapshot = TestPanelSnapshot.From(state, "Example.slnx");

        // Assert
        Assert.Equal("1 test passed", snapshot.SelectedOutput);
    }
}
