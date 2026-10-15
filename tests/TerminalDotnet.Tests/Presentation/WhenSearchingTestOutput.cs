using Terminal.Gui.Drawing;
using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
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

    [Fact]
    public void Pressing_n_moves_to_the_next_match()
    {
        // Arrange
        var output = OutputOf(20);
        output.Search("line 1");

        // Act
        output.NewKeyDownEvent(new Key(KeyCode.N));

        // Assert
        Assert.Equal(9, output.Viewport.Y);
    }

    [Fact]
    public void Pressing_shift_n_moves_to_the_previous_match()
    {
        // Arrange
        var output = OutputOf(20);
        output.Search("line 1");
        output.NewKeyDownEvent(new Key(KeyCode.N));
        output.NewKeyDownEvent(new Key(KeyCode.N));

        // Act
        output.NewKeyDownEvent(new Key(KeyCode.N).WithShift);

        // Assert
        Assert.Equal(9, output.Viewport.Y);
    }

    [Fact]
    public void Pressing_n_on_the_last_match_returns_to_the_first()
    {
        // Arrange
        var output = OutputOf(20);
        output.Search("line 2");
        output.NewKeyDownEvent(new Key(KeyCode.N));

        // Act
        output.NewKeyDownEvent(new Key(KeyCode.N));

        // Assert
        Assert.Equal(1, output.Viewport.Y);
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
