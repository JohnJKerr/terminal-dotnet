using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Keyboard;

public sealed class WhenTheTerminalIsSettling
{
    private static readonly TimeSpan Settle = TimeSpan.FromMilliseconds(500);

    [Fact]
    public void It_ignores_the_replies_a_terminal_sends_while_the_panels_appear()
    {
        // Act
        var accepted = StartupInput.Accepts(TimeSpan.FromMilliseconds(234), Settle);

        // Assert
        Assert.False(accepted);
    }

    [Fact]
    public void It_accepts_the_keys_a_reader_presses_once_the_panels_have_appeared()
    {
        // Act
        var accepted = StartupInput.Accepts(TimeSpan.FromMilliseconds(501), Settle);

        // Assert
        Assert.True(accepted);
    }

    [Fact]
    public void It_accepts_a_key_pressed_exactly_as_the_settling_window_closes()
    {
        // Act
        var accepted = StartupInput.Accepts(Settle, Settle);

        // Assert
        Assert.True(accepted);
    }
}
