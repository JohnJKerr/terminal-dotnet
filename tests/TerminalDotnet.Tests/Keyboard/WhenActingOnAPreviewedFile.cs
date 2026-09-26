using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Keyboard;

public sealed class WhenActingOnAPreviewedFile
{
    [Fact]
    public void Pressing_e_opens_the_file_in_the_editor()
    {
        // Act
        var action = PreviewKeyBindings.ActionFor(new Key(KeyCode.E), viewportHeight: 20);

        // Assert
        Assert.Equal(new PreviewAction.Edit(), action);
    }

    [Fact]
    public void Pressing_c_comments_on_the_file()
    {
        // Act
        var action = PreviewKeyBindings.ActionFor(new Key(KeyCode.C), viewportHeight: 20);

        // Assert
        Assert.Equal(new PreviewAction.Comment(), action);
    }

    [Fact]
    public void Pressing_n_moves_to_the_next_file()
    {
        // Act
        var action = PreviewKeyBindings.ActionFor(new Key(KeyCode.N), viewportHeight: 20);

        // Assert
        Assert.Equal(new PreviewAction.StepFile(1), action);
    }

    [Fact]
    public void Pressing_capital_N_moves_to_the_previous_file()
    {
        // Act
        var action = PreviewKeyBindings.ActionFor(
            new Key(KeyCode.N | KeyCode.ShiftMask),
            viewportHeight: 20);

        // Assert
        Assert.Equal(new PreviewAction.StepFile(-1), action);
    }

    [Fact]
    public void Pressing_capital_J_still_scrolls_rather_than_stepping()
    {
        // Act
        var action = PreviewKeyBindings.ActionFor(
            new Key(KeyCode.J | KeyCode.ShiftMask),
            viewportHeight: 20);

        // Assert
        Assert.Equal(new PreviewAction.Scroll(1), action);
    }
}
