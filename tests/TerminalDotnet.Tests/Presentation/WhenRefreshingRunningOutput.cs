using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Presentation;

public sealed class WhenRefreshingRunningOutput
{
    [Fact]
    public void It_waits_before_replacing_output_that_just_changed()
    {
        // Arrange
        var sinceShown = TimeSpan.FromMilliseconds(10);

        // Act
        var replace = RunningOutputRefresh.Replace(
            "Test Output — Cart.Tests",
            "started",
            "Test Output — Cart.Tests",
            "started more",
            sinceShown);

        // Assert
        Assert.False(replace);
    }

    [Fact]
    public void It_replaces_output_once_the_interval_has_passed()
    {
        // Arrange
        var sinceShown = RunningOutputRefresh.Interval;

        // Act
        var replace = RunningOutputRefresh.Replace(
            "Test Output — Cart.Tests",
            "started",
            "Test Output — Cart.Tests",
            "started more",
            sinceShown);

        // Assert
        Assert.True(replace);
    }

    [Fact]
    public void It_leaves_unchanged_output_as_it_is()
    {
        // Arrange
        var sinceShown = RunningOutputRefresh.Interval;

        // Act
        var replace = RunningOutputRefresh.Replace(
            "Test Output — Cart.Tests",
            "started",
            "Test Output — Cart.Tests",
            "started",
            sinceShown);

        // Assert
        Assert.False(replace);
    }

    [Fact]
    public void It_replaces_the_title_when_the_batch_changes()
    {
        // Arrange
        var sinceShown = RunningOutputRefresh.Interval;

        // Act
        var replace = RunningOutputRefresh.Replace(
            "Test Output — Cart.Tests (batch 1 of 2)",
            "started",
            "Test Output — Cart.Tests (batch 2 of 2)",
            "started",
            sinceShown);

        // Assert
        Assert.True(replace);
    }
}
