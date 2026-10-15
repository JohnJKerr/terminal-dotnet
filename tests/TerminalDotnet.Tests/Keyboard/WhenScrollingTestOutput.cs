using Terminal.Gui.Drawing;
using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using TerminalDotnet.Terminal;
using TerminalDotnet.Tests.Builders;
using Xunit;

namespace TerminalDotnet.Tests.Keyboard;

public sealed class WhenScrollingTestOutput
{
    [Theory]
    [InlineData(KeyCode.CursorDown)]
    [InlineData(KeyCode.J)]
    public void Pressing_down_moves_one_row_forward(KeyCode key)
    {
        // Arrange
        var output = OutputOf(20);

        // Act
        output.NewKeyDownEvent(new Key(key));

        // Assert
        Assert.Equal(1, output.Viewport.Y);
    }

    [Theory]
    [InlineData(KeyCode.CursorUp)]
    [InlineData(KeyCode.K)]
    public void Pressing_up_moves_one_row_back(KeyCode key)
    {
        // Arrange
        var output = OutputOf(20);
        output.ScrollVertical(3);

        // Act
        output.NewKeyDownEvent(new Key(key));

        // Assert
        Assert.Equal(2, output.Viewport.Y);
    }

    [Fact]
    public void Pressing_page_down_moves_a_screen_forward_keeping_a_row_of_context()
    {
        // Arrange
        var output = OutputOf(20);

        // Act
        output.NewKeyDownEvent(new Key(KeyCode.PageDown));

        // Assert
        Assert.Equal(4, output.Viewport.Y);
    }

    [Fact]
    public void Pressing_page_up_moves_a_screen_back_keeping_a_row_of_context()
    {
        // Arrange
        var output = OutputOf(20);
        output.ScrollVertical(10);

        // Act
        output.NewKeyDownEvent(new Key(KeyCode.PageUp));

        // Assert
        Assert.Equal(6, output.Viewport.Y);
    }

    [Fact]
    public void Pressing_end_moves_to_the_last_screen()
    {
        // Arrange
        var output = OutputOf(20);

        // Act
        output.NewKeyDownEvent(new Key(KeyCode.End));

        // Assert
        Assert.Equal(15, output.Viewport.Y);
    }

    [Fact]
    public void Pressing_home_moves_to_the_first_screen()
    {
        // Arrange
        var output = OutputOf(20);
        output.ScrollVertical(10);

        // Act
        output.NewKeyDownEvent(new Key(KeyCode.Home));

        // Assert
        Assert.Equal(0, output.Viewport.Y);
    }

    private static OutputView OutputOf(int lines)
    {
        var output = GivenA.OutputView(height: 5);
        output.Show(AnsiTestOutput.ToCells(string.Join('\n', Enumerable.Range(1, lines).Select(line => $"line {line}"))));
        return output;
    }
}
