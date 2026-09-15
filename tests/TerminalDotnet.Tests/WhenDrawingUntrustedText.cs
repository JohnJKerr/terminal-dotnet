using Terminal.Gui.Drivers;
using Xunit;

namespace TerminalDotnet.Tests.Terminal;

/// <summary>
/// File contents, diffs, compiler messages and test output all come from the
/// repository being read. A control character written straight to the
/// terminal could set its title, rewrite the clipboard (OSC 52) or move the
/// cursor over what the reader sees, so nothing drawn may carry one through.
/// The app relies on the toolkit's output buffer for that; these pin it down.
/// </summary>
public sealed class WhenDrawingUntrustedText
{
    private const char Escape = (char)0x1B;
    private const char Bell = (char)0x07;
    private const char ControlSequenceIntroducer = (char)0x9B;

    [Fact]
    public void It_draws_no_control_character_from_a_clipboard_escape_sequence()
    {
        // Arrange
        var buffer = new OutputBufferImpl();
        buffer.SetSize(40, 1);
        buffer.Move(0, 0);

        // Act
        buffer.AddStr($"a{Escape}]52;c;SGk={Bell}b{ControlSequenceIntroducer}2Jc");

        // Assert
        Assert.DoesNotContain(DrawnCharacters(buffer), char.IsControl);
    }

    [Fact]
    public void It_shows_the_escape_character_as_a_visible_symbol()
    {
        // Arrange
        var buffer = new OutputBufferImpl();
        buffer.SetSize(4, 1);
        buffer.Move(0, 0);

        // Act
        buffer.AddStr($"{Escape}");

        // Assert
        Assert.Equal("␛", buffer.Contents![0, 0].Grapheme);
    }

    private static IEnumerable<char> DrawnCharacters(OutputBufferImpl buffer) => Enumerable
        .Range(0, buffer.Cols)
        .SelectMany(column => buffer.Contents![0, column].Grapheme ?? "");
}
