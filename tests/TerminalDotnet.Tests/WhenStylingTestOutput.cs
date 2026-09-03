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
}
