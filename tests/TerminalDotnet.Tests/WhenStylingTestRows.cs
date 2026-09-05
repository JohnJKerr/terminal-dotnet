using TerminalDotnet.Explorer;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Testing;

public sealed class WhenStylingTestRows
{
    [Fact]
    public void An_added_suite_takes_the_colour_the_explorer_gives_a_new_file()
    {
        // Act
        var foreground = TestRowAppearance.ForegroundFor(TestNodeOutcome.NotRun, TestNodeUpdate.Added);

        // Assert
        Assert.Equal(
            FileRowAppearance.ForegroundFor(FileRowTone.New, global::Terminal.Gui.Drawing.Color.White),
            foreground);
    }

    [Fact]
    public void An_edited_suite_takes_the_colour_the_explorer_gives_a_modified_file()
    {
        // Act
        var foreground = TestRowAppearance.ForegroundFor(TestNodeOutcome.NotRun, TestNodeUpdate.Edited);

        // Assert
        Assert.Equal(
            FileRowAppearance.ForegroundFor(FileRowTone.Modified, global::Terminal.Gui.Drawing.Color.White),
            foreground);
    }

    [Fact]
    public void An_unchanged_test_stays_plain()
    {
        // Act
        var foreground = TestRowAppearance.ForegroundFor(TestNodeOutcome.NotRun, TestNodeUpdate.Unchanged);

        // Assert
        Assert.Equal(global::Terminal.Gui.Drawing.Color.White, foreground);
    }

    [Fact]
    public void A_failed_test_stays_red_although_its_source_changed()
    {
        // Act
        var foreground = TestRowAppearance.ForegroundFor(TestNodeOutcome.Failed, TestNodeUpdate.Added);

        // Assert
        Assert.Equal(global::Terminal.Gui.Drawing.Color.BrightRed, foreground);
    }

    [Fact]
    public void A_passed_test_stays_green()
    {
        // Act
        var foreground = TestRowAppearance.ForegroundFor(TestNodeOutcome.Passed, TestNodeUpdate.Unchanged);

        // Assert
        Assert.Equal(global::Terminal.Gui.Drawing.Color.BrightGreen, foreground);
    }

    [Fact]
    public void A_running_test_stays_cyan()
    {
        // Act
        var foreground = TestRowAppearance.ForegroundFor(TestNodeOutcome.Running, TestNodeUpdate.Edited);

        // Assert
        Assert.Equal(global::Terminal.Gui.Drawing.Color.BrightCyan, foreground);
    }

    [Fact]
    public void A_skipped_test_stays_yellow()
    {
        // Act
        var foreground = TestRowAppearance.ForegroundFor(TestNodeOutcome.Skipped, TestNodeUpdate.Unchanged);

        // Assert
        Assert.Equal(global::Terminal.Gui.Drawing.Color.BrightYellow, foreground);
    }
}
