using TerminalDotnet.Explorer;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Presentation;

public sealed class WhenShowingATestRunToast
{
    [Fact]
    public void A_run_underway_says_how_many_tests_are_running()
    {
        // Arrange
        var state = new ExplorerState(ExplorerStatus.Running, [], 0, "Running 3 tests...");

        // Act
        var toast = TestRunToast.Running(state, TimeSpan.Zero);

        // Assert
        Assert.EndsWith("Running 3 tests...", toast.Text);
    }

    [Fact]
    public void A_run_underway_stays_visible()
    {
        // Arrange
        var state = new ExplorerState(ExplorerStatus.Running, [], 0, "Running 3 tests...");

        // Act
        var toast = TestRunToast.Running(state, TimeSpan.Zero);

        // Assert
        Assert.False(toast.FadesAway);
    }

    [Fact]
    public void A_passed_run_reports_success()
    {
        // Arrange
        var state = new ExplorerState(ExplorerStatus.Ready, [], 0, "Passed");

        // Act
        var toast = TestRunToast.Finished(state);

        // Assert
        Assert.Equal(new Toast("Tests passed", ToastTone.Succeeded), toast);
    }

    [Fact]
    public void A_failed_run_reports_failure()
    {
        // Arrange
        var state = new ExplorerState(ExplorerStatus.Failed, [], 0, "1 test failed");

        // Act
        var toast = TestRunToast.Finished(state);

        // Assert
        Assert.Equal(new Toast("Test run failed", ToastTone.Failed), toast);
    }

    [Fact]
    public void A_cancelled_run_says_it_was_cancelled()
    {
        // Arrange
        var state = new ExplorerState(ExplorerStatus.Ready, [], 0, "Run cancelled");

        // Act
        var toast = TestRunToast.Finished(state);

        // Assert
        Assert.Equal(new Toast("Test run cancelled", ToastTone.Waiting), toast);
    }
}
