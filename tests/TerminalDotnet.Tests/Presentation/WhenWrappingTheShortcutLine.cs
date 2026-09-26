using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Presentation;

public sealed class WhenWrappingTheShortcutLine
{
    [Fact]
    public void It_keeps_every_shortcut_on_one_line_when_they_fit()
    {
        // Act
        var lines = ShortcutLines.For(["Tab pane", "s search", "q quit"], width: 40);

        // Assert
        Assert.Equal(["Tab pane  s search  q quit"], lines);
    }

    [Fact]
    public void It_carries_what_does_not_fit_onto_the_second_line()
    {
        // Act
        var lines = ShortcutLines.For(["Tab pane", "s search", "q quit"], width: 20);

        // Assert
        Assert.Equal(["Tab pane  s search", "q quit"], lines);
    }

    [Fact]
    public void It_breaks_between_shortcuts_rather_than_through_one()
    {
        // Act
        var lines = ShortcutLines.For(["Enter/e edit", "p preview"], width: 14);

        // Assert
        Assert.Equal(["Enter/e edit", "p preview"], lines);
    }

    [Fact]
    public void It_leaves_out_the_shortcuts_that_both_lines_cannot_hold()
    {
        // Act
        var lines = ShortcutLines.For(["Tab pane", "s search", "q quit"], width: 8);

        // Assert
        Assert.Equal(["Tab pane", "s search"], lines);
    }

    [Fact]
    public void It_keeps_a_shortcut_wider_than_the_line_on_a_line_of_its_own()
    {
        // Act
        var lines = ShortcutLines.For(["f next failure", "q quit"], width: 10);

        // Assert
        Assert.Equal(["f next failure", "q quit"], lines);
    }

    [Fact]
    public void It_keeps_one_line_before_the_width_is_known()
    {
        // Act
        var lines = ShortcutLines.For(["Tab pane", "s search", "q quit"], width: 0);

        // Assert
        Assert.Equal(["Tab pane  s search  q quit"], lines);
    }
}
