using TerminalDotnet.Testing;
using Xunit;

namespace TerminalDotnet.Tests.Testing;

public sealed class WhenSummarizingATestRun
{
    [Fact]
    public void It_counts_each_outcome()
    {
        // Arrange
        var run = new TestRun(false, "Finished",
        [
            Result("Passes", TestOutcome.Passed),
            Result("Fails", TestOutcome.Failed),
            Result("Skips", TestOutcome.Skipped),
            Result("Also skips", TestOutcome.Skipped)
        ]);

        // Act
        var summary = run.Summary;

        // Assert
        Assert.Equal(new TestRunSummary(1, 1, 2), summary);
    }

    [Fact]
    public void It_counts_nothing_when_no_test_reported_a_result()
    {
        // Arrange
        var run = new TestRun(false, "The build failed");

        // Act
        var summary = run.Summary;

        // Assert
        Assert.Equal(new TestRunSummary(0, 0, 0), summary);
    }

    private static TestResult Result(string name, TestOutcome outcome) => new(
        new TestCase($"Shop.Tests.CartTests.{name}", name, "Shop.Tests.csproj"),
        outcome,
        TimeSpan.Zero,
        null,
        null,
        null,
        null);
}
