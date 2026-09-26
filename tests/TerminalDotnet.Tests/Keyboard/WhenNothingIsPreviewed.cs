using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Keyboard;

/// <summary>A folder, a project or a suite has no file to show, but the
/// reader stepping through the panel from the preview has to be able to step
/// past it.</summary>
public sealed class WhenNothingIsPreviewed
{
    [Fact]
    public void Pressing_n_still_moves_to_the_next_row()
    {
        // Act
        var action = PreviewKeyBindings.ActionFor(new Key(KeyCode.N), viewportHeight: 20, showsAFile: false);

        // Assert
        Assert.Equal(new PreviewAction.StepFile(1), action);
    }

    [Fact]
    public void Pressing_shift_n_still_moves_to_the_previous_row()
    {
        // Act
        var action = PreviewKeyBindings.ActionFor(
            new Key(KeyCode.N | KeyCode.ShiftMask),
            viewportHeight: 20,
            showsAFile: false);

        // Assert
        Assert.Equal(new PreviewAction.StepFile(-1), action);
    }

    [Fact]
    public void Pressing_e_has_no_file_to_open()
    {
        // Act
        var action = PreviewKeyBindings.ActionFor(new Key(KeyCode.E), viewportHeight: 20, showsAFile: false);

        // Assert
        Assert.Null(action);
    }

    [Fact]
    public void Pressing_c_has_no_file_to_comment_on()
    {
        // Act
        var action = PreviewKeyBindings.ActionFor(new Key(KeyCode.C), viewportHeight: 20, showsAFile: false);

        // Assert
        Assert.Null(action);
    }
}
