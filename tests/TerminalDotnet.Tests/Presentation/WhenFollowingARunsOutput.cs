using Terminal.Gui.Drawing;
using TerminalDotnet.Terminal;
using TerminalDotnet.Tests.Builders;
using Xunit;

namespace TerminalDotnet.Tests.Presentation;

public sealed class WhenFollowingARunsOutput
{
    [Fact]
    public void It_opens_at_the_end_of_the_output()
    {
        // Arrange
        var output = GivenA.OutputView(height: 5, followsTheRun: true);

        // Act
        output.Show(Lines(20));

        // Assert
        Assert.Equal(15, output.Viewport.Y);
    }

    [Fact]
    public void Shorter_output_keeps_its_end_in_view()
    {
        // Arrange
        var output = GivenA.OutputView(height: 5, followsTheRun: true);
        output.Show(Lines(20));

        // Act
        output.Show(Lines(8));

        // Assert
        Assert.Equal(3, output.Viewport.Y);
    }

    [Fact]
    public void A_reader_scrolled_back_keeps_their_place_as_output_arrives()
    {
        // Arrange
        var output = GivenA.OutputView(height: 5, followsTheRun: true);
        output.Show(Lines(20));
        output.ScrollVertical(-5);

        // Act
        output.Show(Lines(25));

        // Assert
        Assert.Equal(10, output.Viewport.Y);
    }

    private static List<List<Cell>> Lines(int count) =>
        AnsiTestOutput.ToCells(string.Join('\n', Enumerable.Range(1, count).Select(line => $"line {line}")));
}
