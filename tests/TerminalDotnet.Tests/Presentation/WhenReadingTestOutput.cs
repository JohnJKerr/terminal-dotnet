using TerminalDotnet.Terminal;
using TerminalDotnet.Tests.Builders;
using Xunit;

namespace TerminalDotnet.Tests.Presentation;

public sealed class WhenReadingTestOutput
{
    [Fact]
    public void A_line_wider_than_the_view_wraps_onto_the_rows_beneath()
    {
        // Arrange
        var output = GivenA.OutputView(width: 10);

        // Act
        output.Show(AnsiTestOutput.ToCells(new string('x', 25)));

        // Assert
        Assert.Equal(3, output.GetContentSize().Height);
    }
}
