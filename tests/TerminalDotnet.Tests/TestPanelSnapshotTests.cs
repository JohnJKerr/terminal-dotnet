using TerminalDotnet.Explorer;
using TerminalDotnet.Terminal;
using TerminalDotnet.Testing;
using Xunit;

namespace TerminalDotnet.Tests;

public class WhenCreatingATestPanelSnapshot
{
    [Fact]
    public void It_exposes_execution_output_as_scrollable_lines()
    {
        // Arrange
        var state = new ExplorerState(
            ExplorerStatus.Ready,
            [],
            0,
            "first line\nsecond line");

        // Act
        var snapshot = TestPanelSnapshot.From(state, "Example.slnx");

        // Assert
        Assert.Equal(["first line", "second line"], snapshot.OutputLines.Select(line => line.Text));
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
    public void It_marks_failed_output_as_failure()
    {
        // Arrange
        var state = new ExplorerState(
            ExplorerStatus.Failed,
            [],
            0,
            "Failed! - Failed: 1, Passed: 0");

        // Act
        var snapshot = TestPanelSnapshot.From(state, "Example.slnx");

        // Assert
        Assert.Equal(OutputLineTone.Failure, snapshot.OutputLines[0].Tone);
    }

    [Fact]
    public void It_exposes_failure_details_separately_from_execution_output()
    {
        // Arrange
        var test = new TestCase("Shop.Tests.CartTests.Adds_item", "Adds item", "Shop.Tests.csproj");
        var failure = new TestResult(
            test,
            TestOutcome.Failed,
            TimeSpan.FromMilliseconds(7),
            "Expected total to be 10.",
            null,
            "/repo/CartTests.cs",
            42);
        var state = new ExplorerState(
            ExplorerStatus.Failed,
            [new VisibleTestNode(2, TestNodeKind.Test, test.DisplayName, [test], TestNodeOutcome.Failed)],
            0,
            "raw dotnet output",
            new TestRun(false, "raw dotnet output", [failure]));

        // Act
        var snapshot = TestPanelSnapshot.From(state, "Example.slnx");

        // Assert
        Assert.Equal(
        [
            "✗ Adds item — Expected total to be 10.",
            "/repo/CartTests.cs:42"
        ], snapshot.ResultLines.Select(line => line.Text));
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

    [Theory]
    [InlineData("Passed! - Passed: 12", OutputLineTone.Success)]
    [InlineData("Skipped: 2", OutputLineTone.Skipped)]
    [InlineData("Running 12 tests...", OutputLineTone.Status)]
    public void It_marks_test_status_output_with_its_tone(string output, OutputLineTone expectedTone)
    {
        // Arrange
        var state = new ExplorerState(
            ExplorerStatus.Ready,
            [],
            0,
            output);

        // Act
        var snapshot = TestPanelSnapshot.From(state, "Example.slnx");

        // Assert
        Assert.Equal(expectedTone, snapshot.OutputLines[0].Tone);
    }
}
