using TerminalDotnet.Explorer;
using TerminalDotnet.Terminal;
using TerminalDotnet.Testing;
using TerminalDotnet.Tests.Builders;
using Xunit;

namespace TerminalDotnet.Tests.Presentation;

public class WhenCreatingATestPanelSnapshot
{
    [Fact]
    public void It_recounts_when_the_visible_tests_change()
    {
        // Arrange
        var state = new ExplorerState(ExplorerStatus.Ready, [], 0, "Ready");
        var test = new TestCase("Shop.Tests.CartTests.Adds_item", "Adds item", "Shop.Tests.csproj");

        // Act
        var updated = state with
        {
            VisibleNodes = [new VisibleTestNode(0, TestNodeKind.Test, test.DisplayName, [test])]
        };
        var snapshot = TestPanelSnapshot.From(updated, "Example.slnx");

        // Assert
        Assert.Equal(1, snapshot.SearchHitCount);
    }

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
        var result = GivenA.ResultFor(test).Taking(TimeSpan.FromMilliseconds(7)).Build();
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
            GivenA.ResultFor(passed).Build(),
            GivenA.ResultFor(failed).Failed().Build(),
            GivenA.ResultFor(skipped).Skipped().Build()
        ]);
        var state = new ExplorerState(
            ExplorerStatus.Failed,
            [new VisibleTestNode(0, TestNodeKind.Project, "Shop.Tests", [passed, failed, skipped])],
            0,
            "Finished",
            run) { DiscoveredTestCount = 12 };

        // Act
        var snapshot = TestPanelSnapshot.From(state, "Shop.slnx");

        // Assert
        Assert.Equal("1 Failed, 1 Passed, 1 Skipped, 12 Total", snapshot.StatusLine);
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
        var result = GivenA.ResultFor(test).WithOutput("Cart total: 10").Build();
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
