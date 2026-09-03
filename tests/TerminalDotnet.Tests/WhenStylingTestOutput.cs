using Terminal.Gui.Drawing;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Testing;

public sealed class WhenStylingTestOutput
{
    [Fact]
    public void It_renders_dotnet_ansi_colors_on_a_black_background()
    {
        // Arrange
        const string output = "Build \e[31;1mfailed\e[m";

        // Act
        var failed = AnsiTestOutput.ToCells(output)[0][6];

        // Assert
        Assert.Equal(("f", Color.BrightRed, Color.Black), (failed.Grapheme, failed.Attribute!.Value.Foreground, failed.Attribute.Value.Background));
    }

    [Fact]
    public void It_keeps_a_failure_report_verbatim()
    {
        // Arrange
        const string output = "    /repo/CartTests.cs(11): \e[31;1merror\e[m \e[31;1mTESTERROR\e[m: ";

        // Act
        var line = AnsiTestOutput.ToCells(output).Single();

        // Assert
        Assert.Equal("    /repo/CartTests.cs(11): error TESTERROR: ", Text(line));
    }

    [Fact]
    public void It_keeps_the_run_summary()
    {
        // Arrange
        const string output = "Test summary: total: 6, failed: 1, succeeded: 5, skipped: 0, duration: 1.6s";

        // Act
        var line = AnsiTestOutput.ToCells(output).Single();

        // Assert
        Assert.Equal(output, Text(line));
    }

    [Fact]
    public void It_hides_the_restore_stopwatch()
    {
        // Arrange
        const string output = "Restore complete (0.5s)\nTest summary: total: 6";

        // Act
        var lines = AnsiTestOutput.ToCells(output);

        // Assert
        Assert.Equal(["Test summary: total: 6"], lines.Select(Text));
    }

    [Fact]
    public void It_hides_a_projects_build_stopwatch()
    {
        // Arrange
        const string output = "  Shop.Tests \e[36;1mnet10.0\e[m \e[32;1msucceeded\e[m (0.2s) → bin/Debug/net10.0/Shop.Tests.dll";

        // Act
        var lines = AnsiTestOutput.ToCells(output);

        // Assert
        Assert.Empty(lines);
    }

    [Fact]
    public void It_keeps_a_build_stopwatch_that_reports_a_failure()
    {
        // Arrange
        const string output = "  Shop.Tests test \e[36;1mnet10.0\e[m \e[31;1mfailed with 1 error(s)\e[m (0.9s)";

        // Act
        var line = AnsiTestOutput.ToCells(output).Single();

        // Assert
        Assert.Equal("  Shop.Tests test net10.0 failed with 1 error(s) (0.9s)", Text(line));
    }

    [Fact]
    public void It_hides_lines_that_carry_no_text()
    {
        // Arrange
        const string output = "\e]9;4;0;\e\\";

        // Act
        var lines = AnsiTestOutput.ToCells(output);

        // Assert
        Assert.Empty(lines);
    }

    [Fact]
    public void It_keeps_the_blank_line_before_the_run_summary()
    {
        // Arrange
        const string output = "Results File: /tmp/run.trx\n\nTest summary: total: 6";

        // Act
        var lines = AnsiTestOutput.ToCells(output);

        // Assert
        Assert.Equal(3, lines.Count);
    }

    [Fact]
    public void It_keeps_the_link_text_of_a_hyperlinked_assembly()
    {
        // Arrange
        const string output = "\e]8;;file:///repo/bin/Shop.Tests.dll\e\\bin/Shop.Tests.dll\e]8;;\e\\ passed";

        // Act
        var line = AnsiTestOutput.ToCells(output).Single();

        // Assert
        Assert.Equal("bin/Shop.Tests.dll passed", Text(line));
    }

    private static string Text(IEnumerable<Cell> line) => string.Concat(line.Select(cell => cell.Grapheme));
}
