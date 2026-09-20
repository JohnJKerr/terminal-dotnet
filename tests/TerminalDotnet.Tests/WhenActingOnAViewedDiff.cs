using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Terminal;

public sealed class WhenActingOnAViewedDiff
{
    [Fact]
    public void Pressing_n_shows_the_next_files_diff()
    {
        // Act
        var action = DiffKeyBindings.ActionFor(new Key(KeyCode.N));

        // Assert
        Assert.Equal(new DiffAction.StepFile(1), action);
    }

    [Fact]
    public void Pressing_capital_N_shows_the_previous_files_diff()
    {
        // Act
        var action = DiffKeyBindings.ActionFor(new Key(KeyCode.N | KeyCode.ShiftMask));

        // Assert
        Assert.Equal(new DiffAction.StepFile(-1), action);
    }

    [Fact]
    public void Pressing_j_scrolls_down_a_line()
    {
        // Act
        var action = DiffKeyBindings.ActionFor(new Key(KeyCode.J));

        // Assert
        Assert.Equal(new DiffAction.Scroll(1), action);
    }

    [Fact]
    public void Pressing_k_scrolls_up_a_line()
    {
        // Act
        var action = DiffKeyBindings.ActionFor(new Key(KeyCode.K));

        // Assert
        Assert.Equal(new DiffAction.Scroll(-1), action);
    }

    [Fact]
    public void Pressing_c_comments_on_the_file()
    {
        // Act
        var action = DiffKeyBindings.ActionFor(new Key(KeyCode.C));

        // Assert
        Assert.Equal(new DiffAction.Comment(), action);
    }

    [Fact]
    public void Pressing_a_key_the_diff_has_nothing_for_leaves_it_to_the_view()
    {
        // Act
        var action = DiffKeyBindings.ActionFor(new Key(KeyCode.PageDown));

        // Assert
        Assert.Null(action);
    }
}
