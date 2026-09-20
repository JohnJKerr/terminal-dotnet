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
        var action = DiffKeyBindings.ActionFor(new Key(KeyCode.N), viewportHeight: 20);

        // Assert
        Assert.Equal(new DiffAction.StepFile(1), action);
    }

    [Fact]
    public void Pressing_capital_N_shows_the_previous_files_diff()
    {
        // Act
        var action = DiffKeyBindings.ActionFor(
            new Key(KeyCode.N | KeyCode.ShiftMask),
            viewportHeight: 20);

        // Assert
        Assert.Equal(new DiffAction.StepFile(-1), action);
    }

    [Fact]
    public void Pressing_j_scrolls_down_a_line()
    {
        // Act
        var action = DiffKeyBindings.ActionFor(new Key(KeyCode.J), viewportHeight: 20);

        // Assert
        Assert.Equal(new DiffAction.Scroll(1), action);
    }

    [Fact]
    public void Pressing_k_scrolls_up_a_line()
    {
        // Act
        var action = DiffKeyBindings.ActionFor(new Key(KeyCode.K), viewportHeight: 20);

        // Assert
        Assert.Equal(new DiffAction.Scroll(-1), action);
    }

    [Fact]
    public void Pressing_the_down_arrow_scrolls_down_a_line()
    {
        // Act
        var action = DiffKeyBindings.ActionFor(
            new Key(KeyCode.CursorDown),
            viewportHeight: 20);

        // Assert
        Assert.Equal(new DiffAction.Scroll(1), action);
    }

    [Fact]
    public void Pressing_the_up_arrow_scrolls_up_a_line()
    {
        // Act
        var action = DiffKeyBindings.ActionFor(
            new Key(KeyCode.CursorUp),
            viewportHeight: 20);

        // Assert
        Assert.Equal(new DiffAction.Scroll(-1), action);
    }

    [Fact]
    public void Pressing_page_down_moves_a_screen_forward()
    {
        // Act
        var action = DiffKeyBindings.ActionFor(new Key(KeyCode.PageDown), viewportHeight: 20);

        // Assert
        Assert.Equal(new DiffAction.Scroll(19), action);
    }

    [Fact]
    public void Pressing_page_up_moves_a_screen_back()
    {
        // Act
        var action = DiffKeyBindings.ActionFor(new Key(KeyCode.PageUp), viewportHeight: 20);

        // Assert
        Assert.Equal(new DiffAction.Scroll(-19), action);
    }

    [Fact]
    public void Pressing_c_comments_on_the_file()
    {
        // Act
        var action = DiffKeyBindings.ActionFor(new Key(KeyCode.C), viewportHeight: 20);

        // Assert
        Assert.Equal(new DiffAction.Comment(), action);
    }

    [Fact]
    public void Pressing_a_key_the_diff_has_nothing_for_leaves_it_to_the_view()
    {
        // Act
        var action = DiffKeyBindings.ActionFor(new Key(KeyCode.Q), viewportHeight: 20);

        // Assert
        Assert.Null(action);
    }
}
