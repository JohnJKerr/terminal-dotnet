using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Keyboard;

public sealed class WhenScrollingAPreview
{
    [Fact]
    public void Pressing_the_down_arrow_moves_one_row_forward()
    {
        // Act
        var action = PreviewKeyBindings.ActionFor(new Key(KeyCode.CursorDown), viewportHeight: 20, showsAFile: true);

        // Assert
        Assert.Equal(new PreviewAction.Scroll(1), action);
    }

    [Fact]
    public void Pressing_the_up_arrow_moves_one_row_back()
    {
        // Act
        var action = PreviewKeyBindings.ActionFor(new Key(KeyCode.CursorUp), viewportHeight: 20, showsAFile: true);

        // Assert
        Assert.Equal(new PreviewAction.Scroll(-1), action);
    }

    [Fact]
    public void Pressing_j_moves_one_row_forward()
    {
        // Act
        var action = PreviewKeyBindings.ActionFor(new Key(KeyCode.J), viewportHeight: 20, showsAFile: true);

        // Assert
        Assert.Equal(new PreviewAction.Scroll(1), action);
    }

    [Fact]
    public void Pressing_k_moves_one_row_back()
    {
        // Act
        var action = PreviewKeyBindings.ActionFor(new Key(KeyCode.K), viewportHeight: 20, showsAFile: true);

        // Assert
        Assert.Equal(new PreviewAction.Scroll(-1), action);
    }

    [Fact]
    public void Pressing_page_down_moves_a_screen_forward()
    {
        // Act
        var action = PreviewKeyBindings.ActionFor(new Key(KeyCode.PageDown), viewportHeight: 20, showsAFile: true);

        // Assert
        Assert.Equal(new PreviewAction.Scroll(19), action);
    }

    [Fact]
    public void Pressing_page_up_moves_a_screen_back()
    {
        // Act
        var action = PreviewKeyBindings.ActionFor(new Key(KeyCode.PageUp), viewportHeight: 20, showsAFile: true);

        // Assert
        Assert.Equal(new PreviewAction.Scroll(-19), action);
    }

    [Fact]
    public void Paging_a_viewport_too_short_to_overlap_still_moves_a_row()
    {
        // Act
        var action = PreviewKeyBindings.ActionFor(new Key(KeyCode.PageDown), viewportHeight: 1, showsAFile: true);

        // Assert
        Assert.Equal(new PreviewAction.Scroll(1), action);
    }

    [Fact]
    public void Pressing_home_returns_to_the_first_line()
    {
        // Act
        var action = PreviewKeyBindings.ActionFor(new Key(KeyCode.Home), viewportHeight: 20, showsAFile: true);

        // Assert
        Assert.Equal(new PreviewAction.ScrollToStart(), action);
    }

    [Fact]
    public void Pressing_end_jumps_to_the_last_line()
    {
        // Act
        var action = PreviewKeyBindings.ActionFor(new Key(KeyCode.End), viewportHeight: 20, showsAFile: true);

        // Assert
        Assert.Equal(new PreviewAction.ScrollToEnd(), action);
    }

    [Fact]
    public void Pressing_a_key_the_preview_does_not_answer_to_leaves_it_alone()
    {
        // Act
        var action = PreviewKeyBindings.ActionFor(new Key(KeyCode.Q), viewportHeight: 20, showsAFile: true);

        // Assert
        Assert.Null(action);
    }
}
