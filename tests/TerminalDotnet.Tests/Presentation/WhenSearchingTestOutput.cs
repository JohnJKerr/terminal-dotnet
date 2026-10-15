using Terminal.Gui.Drawing;
using TerminalDotnet.Terminal;
using TerminalDotnet.Tests.Builders;
using Xunit;

namespace TerminalDotnet.Tests.Presentation;

public sealed class WhenSearchingTestOutput
{
    [Fact]
    public void It_brings_the_first_match_to_the_top()
    {
        // Arrange
        var output = OutputOf(20);

        // Act
        output.Search("line 12");

        // Assert
        Assert.Equal(11, output.Viewport.Y);
    }

    [Fact]
    public void It_matches_regardless_of_case()
    {
        // Arrange
        var output = OutputOf(20);

        // Act
        output.Search("LINE 12");

        // Assert
        Assert.Equal(11, output.Viewport.Y);
    }

    private static OutputView OutputOf(int lines)
    {
        var output = GivenA.OutputView(height: 5);
        output.Show(Lines(lines));
        return output;
    }

    private static List<List<Cell>> Lines(int count) =>
        AnsiTestOutput.ToCells(string.Join('\n', Enumerable.Range(1, count).Select(line => $"line {line}")));
}
