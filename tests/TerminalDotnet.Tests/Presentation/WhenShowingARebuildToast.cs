using TerminalDotnet.Explorer;
using TerminalDotnet.Issues;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Presentation;

/// <summary>A rebuild can be set off from any panel, so what it is doing and
/// what it found are told in a toast over the panels rather than in the small
/// print of the two panels it reloads.</summary>
public sealed class WhenShowingARebuildToast
{
    private static readonly CompilationIssue Broken =
        new("/repo/Cart.cs", "Cart.cs", 3, 1, "CS1002", "; expected", IssueSeverity.Error);

    private static readonly CompilationIssue Unused =
        new("/repo/Cart.cs", "Cart.cs", 9, 5, "CS0168", "unused", IssueSeverity.Warning);

    [Fact]
    public void A_rebuild_underway_says_so()
    {
        // Act
        var toast = RebuildToast.Rebuilding(TimeSpan.Zero);

        // Assert
        Assert.EndsWith("Rebuilding...", toast.Text);
    }

    [Fact]
    public void It_stays_up_for_as_long_as_the_rebuild_takes()
    {
        // Act
        var toast = RebuildToast.Rebuilding(TimeSpan.Zero);

        // Assert
        Assert.False(toast.FadesAway);
    }

    [Fact]
    public void A_clean_build_reports_what_it_found()
    {
        // Act
        var toast = RebuildToast.Finished(Issues(Unused), Tests(ExplorerStatus.Ready));

        // Assert
        Assert.Equal("Built — 0 errors, 1 warning · tests rediscovered", toast.Text);
    }

    [Fact]
    public void A_clean_build_reads_as_a_success()
    {
        // Act
        var toast = RebuildToast.Finished(Issues(Unused), Tests(ExplorerStatus.Ready));

        // Assert
        Assert.Equal(ToastTone.Succeeded, toast.Tone);
    }

    [Fact]
    public void A_broken_build_says_the_tests_were_left_as_they_were()
    {
        // Act
        var toast = RebuildToast.Finished(Issues(Broken, Broken with { Line = 8 }), Tests(ExplorerStatus.Ready));

        // Assert
        Assert.Equal("Build failed — 2 errors, 0 warnings · tests left as they were", toast.Text);
    }

    [Fact]
    public void A_broken_build_reads_as_a_failure()
    {
        // Act
        var toast = RebuildToast.Finished(Issues(Broken), Tests(ExplorerStatus.Ready));

        // Assert
        Assert.Equal(ToastTone.Failed, toast.Tone);
    }

    [Fact]
    public void A_discovery_that_failed_says_so()
    {
        // Act
        var toast = RebuildToast.Finished(Issues(), Tests(ExplorerStatus.Failed));

        // Assert
        Assert.Equal("Built — 0 errors, 0 warnings · test discovery failed", toast.Text);
    }

    [Fact]
    public void What_it_found_fades_away_on_its_own()
    {
        // Act
        var toast = RebuildToast.Finished(Issues(), Tests(ExplorerStatus.Ready));

        // Assert
        Assert.True(toast.FadesAway);
    }

    [Fact]
    public void A_rebuild_waiting_on_a_run_says_why()
    {
        // Act
        var toast = RebuildToast.WaitingOnTheRun();

        // Assert
        Assert.Equal("Rebuild waits for the test run to finish", toast.Text);
    }

    private static IssueState Issues(params CompilationIssue[] found) => new(found) { Loading = false };

    private static ExplorerState Tests(ExplorerStatus status) => new(status, [], 0, "");
}
