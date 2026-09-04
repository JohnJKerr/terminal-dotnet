using Terminal.Gui.Drawing;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Changeset;

public sealed class WhenStylingADiff
{
    private const string Diff = """
        diff --git a/src/Order.cs b/src/Order.cs
        index 1234567..89abcde 100644
        --- a/src/Order.cs
        +++ b/src/Order.cs
        @@ -1,3 +1,3 @@
         public sealed class Order
        -    public int Total;
        +    public decimal Total;
        """;

    [Fact]
    public void It_tones_each_line_by_the_part_of_the_diff_it_carries()
    {
        // Arrange
        var diff = Diff;

        // Act
        var lines = DiffAppearance.LinesFrom(diff);

        // Assert
        Assert.Equal(
        [
            DiffLineTone.Meta,
            DiffLineTone.Meta,
            DiffLineTone.Meta,
            DiffLineTone.Meta,
            DiffLineTone.Hunk,
            DiffLineTone.Context,
            DiffLineTone.Removed,
            DiffLineTone.Added
        ],
        lines.Select(line => line.Tone));
    }

    [Fact]
    public void It_keeps_every_line_of_the_diff_verbatim()
    {
        // Arrange
        var diff = Diff;

        // Act
        var lines = DiffAppearance.LinesFrom(diff);

        // Assert
        Assert.Equal(Diff.Split('\n'), lines.Select(line => line.Text));
    }

    [Fact]
    public void It_reads_no_lines_from_an_empty_diff()
    {
        // Arrange
        var diff = "";

        // Act
        var lines = DiffAppearance.LinesFrom(diff);

        // Assert
        Assert.Empty(lines);
    }

    [Fact]
    public void It_paints_inserted_lines_green_and_removed_lines_red()
    {
        // Arrange
        var tones = new[] { DiffLineTone.Added, DiffLineTone.Removed };

        // Act
        var colors = tones.Select(DiffAppearance.ForegroundFor);

        // Assert
        Assert.Equal([Color.BrightGreen, Color.BrightRed], colors);
    }
}
