using System.Drawing;
using Terminal.Gui.Input;
using TerminalDotnet.Terminal;
using TerminalDotnet.Tests.Builders;
using Xunit;

namespace TerminalDotnet.Tests.Mouse;

public sealed class WhenWheelingOverTestOutput
{
    [Fact]
    public void Wheeling_down_moves_the_output_forward()
    {
        // Arrange
        var output = OutputOf(20);

        // Act
        Wheel(output, MouseFlags.WheeledDown);

        // Assert
        Assert.Equal(3, output.Viewport.Y);
    }

    [Fact]
    public void Wheeling_up_moves_the_output_back()
    {
        // Arrange
        var output = OutputOf(20);
        output.ScrollVertical(10);

        // Act
        Wheel(output, MouseFlags.WheeledUp);

        // Assert
        Assert.Equal(7, output.Viewport.Y);
    }

    private static void Wheel(OutputView output, MouseFlags direction) =>
        output.NewMouseEvent(new global::Terminal.Gui.Input.Mouse { Flags = direction, Position = new Point(2, 2), View = output });

    private static OutputView OutputOf(int lines)
    {
        var output = GivenA.OutputView(height: 5);
        output.Show(AnsiTestOutput.ToCells(string.Join('\n', Enumerable.Range(1, lines).Select(line => $"line {line}"))));
        return output;
    }
}
