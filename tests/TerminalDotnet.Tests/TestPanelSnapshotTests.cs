using TerminalDotnet.Explorer;
using TerminalDotnet.Terminal;
using TerminalDotnet.Testing;
using Xunit;

namespace TerminalDotnet.Tests;

public class WhenCreatingATestPanelSnapshot
{
    [Fact]
    public void It_displays_the_selected_test_result()
    {
        // Arrange
        var selectedTest = Test("Example.Tests.FailingTest", "Failing test");
        var otherTest = Test("Example.Tests.PassingTest", "Passing test");
        var result = new TestResult(
            selectedTest,
            TestOutcome.Failed,
            TimeSpan.FromMilliseconds(12),
            "Expected true",
            "at Example.Tests.FailingTest()",
            null,
            null);
        var state = new ExplorerState(
            ExplorerStatus.Failed,
            [
                new VisibleTestNode(2, TestNodeKind.Test, otherTest.DisplayName, [otherTest]),
                new VisibleTestNode(2, TestNodeKind.Test, selectedTest.DisplayName, [selectedTest], TestNodeOutcome.Failed)
            ],
            1,
            "raw dotnet output",
            new TestRun(false, "raw dotnet output", [result]));

        // Act
        var snapshot = TestPanelSnapshot.From(state, "Example.slnx");

        // Assert
        Assert.Equal("Failing test", snapshot.Result.Title);
    }

    [Fact]
    public void It_keeps_raw_command_output_separate_from_the_result()
    {
        // Arrange
        var state = new ExplorerState(
            ExplorerStatus.Ready,
            [],
            0,
            "raw dotnet output");

        // Act
        var snapshot = TestPanelSnapshot.From(state, "Example.slnx");

        // Assert
        Assert.Equal("raw dotnet output", snapshot.Output);
    }

    [Fact]
    public void It_displays_the_failure_source_location()
    {
        // Arrange
        var selectedTest = Test("Example.Tests.FailingTest", "Failing test");
        var result = new TestResult(
            selectedTest,
            TestOutcome.Failed,
            TimeSpan.FromMilliseconds(12),
            "Expected true",
            null,
            "/repo/FailingTest.cs",
            42);
        var state = new ExplorerState(
            ExplorerStatus.Failed,
            [new VisibleTestNode(2, TestNodeKind.Test, selectedTest.DisplayName, [selectedTest])],
            0,
            "raw dotnet output",
            new TestRun(false, "raw dotnet output", [result]));

        // Act
        var snapshot = TestPanelSnapshot.From(state, "Example.slnx");

        // Assert
        Assert.Contains("/repo/FailingTest.cs:42", snapshot.Result.Details);
    }

    private static TestCase Test(string fullyQualifiedName, string displayName) =>
        new(fullyQualifiedName, displayName, "/repo/Example.Tests.csproj");
}
