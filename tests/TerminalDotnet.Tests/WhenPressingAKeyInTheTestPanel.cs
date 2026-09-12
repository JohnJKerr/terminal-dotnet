using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using TerminalDotnet.Explorer;
using TerminalDotnet.Filters;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Testing;

public sealed class WhenPressingAKeyInTheTestPanel
{
    [Fact]
    public void Pressing_j_moves_down()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.J));

        // Assert
        Assert.Equal(new TestPanelAction.Dispatch(new ExplorerCommand.MoveDown()), action);
    }

    [Fact]
    public void Pressing_k_moves_up()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.K));

        // Assert
        Assert.Equal(new TestPanelAction.Dispatch(new ExplorerCommand.MoveUp()), action);
    }

    [Fact]
    public void Pressing_space_folds_the_selection()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.Space));

        // Assert
        Assert.Equal(new TestPanelAction.Dispatch(new ExplorerCommand.ToggleExpanded()), action);
    }

    [Fact]
    public void Pressing_z_folds_the_whole_tree()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.Z));

        // Assert
        Assert.Equal(new TestPanelAction.Dispatch(new ExplorerCommand.ToggleAllExpanded()), action);
    }

    [Fact]
    public void Pressing_enter_runs_the_selection()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.Enter));

        // Assert
        Assert.Equal(new TestPanelAction.Dispatch(new ExplorerCommand.RunSelected()), action);
    }

    [Fact]
    public void Pressing_shift_r_reruns_the_last_run()
    {
        // Act
        var action = ActionFor(Shifted(KeyCode.R));

        // Assert
        Assert.Equal(new TestPanelAction.Dispatch(new ExplorerCommand.RerunLast()), action);
    }

    [Fact]
    public void Pressing_shift_f_reruns_the_failures()
    {
        // Act
        var action = ActionFor(Shifted(KeyCode.F));

        // Assert
        Assert.Equal(new TestPanelAction.Dispatch(new ExplorerCommand.RerunFailed()), action);
    }

    [Fact]
    public void Pressing_c_cancels_the_run()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.C));

        // Assert
        Assert.Equal(new TestPanelAction.CancelRun(), action);
    }

    [Fact]
    public void Pressing_e_opens_the_source()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.E));

        // Assert
        Assert.Equal(new TestPanelAction.OpenSource(), action);
    }

    [Fact]
    public void Pressing_p_previews_the_source()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.P));

        // Assert
        Assert.Equal(new TestPanelAction.PreviewSource(), action);
    }

    [Fact]
    public void Pressing_o_shows_the_output()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.O));

        // Assert
        Assert.Equal(new TestPanelAction.ShowOutput(), action);
    }

    [Fact]
    public void Pressing_o_while_the_panels_have_focus_still_shows_the_output()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.O), hasFocus: false);

        // Assert
        Assert.Equal(new TestPanelAction.ShowOutput(), action);
    }

    [Fact]
    public void Pressing_j_while_the_panels_have_focus_does_nothing()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.J), hasFocus: false);

        // Assert
        Assert.Null(action);
    }

    [Fact]
    public void Pressing_n_during_a_search_moves_to_the_next_match()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.N), searchQuery: "cart");

        // Assert
        Assert.Equal(new TestPanelAction.Dispatch(new ExplorerCommand.NextSearchMatch()), action);
    }

    [Fact]
    public void Pressing_shift_n_during_a_search_moves_to_the_previous_match()
    {
        // Act
        var action = ActionFor(Shifted(KeyCode.N), searchQuery: "cart");

        // Assert
        Assert.Equal(new TestPanelAction.Dispatch(new ExplorerCommand.PreviousSearchMatch()), action);
    }

    [Fact]
    public void Pressing_n_without_a_search_does_nothing()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.N));

        // Assert
        Assert.Null(action);
    }

    [Fact]
    public void Pressing_g_then_f_moves_to_the_next_failure()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.F), awaitingNavigation: true);

        // Assert
        Assert.Equal(new TestPanelAction.Dispatch(new ExplorerCommand.NextFailure()), action);
    }

    [Fact]
    public void It_keeps_waiting_after_jumping_to_a_failure()
    {
        // Arrange
        var action = ActionFor(new Key(KeyCode.F), awaitingNavigation: true);

        // Act
        var keepsWaiting = TestPanelKeyBindings.ContinuesNavigating(action!);

        // Assert
        Assert.True(keepsWaiting);
    }

    [Fact]
    public void It_gives_up_waiting_after_anything_else()
    {
        // Arrange
        var action = ActionFor(new Key(KeyCode.P));

        // Act
        var keepsWaiting = TestPanelKeyBindings.ContinuesNavigating(action!);

        // Assert
        Assert.False(keepsWaiting);
    }

    [Fact]
    public void Pressing_1_toggles_the_updated_filter()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.D1));

        // Assert
        Assert.Equal(
            new TestPanelAction.Dispatch(new ExplorerCommand.ToggleFilter(ExplorerFilter.Updated)),
            action);
    }

    [Fact]
    public void Pressing_1_without_focus_does_nothing()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.D1), hasFocus: false);

        // Assert
        Assert.Null(action);
    }

    private static Key Shifted(KeyCode keyCode) => new(keyCode | KeyCode.ShiftMask);

    private static TestPanelAction? ActionFor(
        Key key,
        string searchQuery = "",
        bool hasFocus = true,
        bool awaitingNavigation = false) =>
        TestPanelKeyBindings.ActionFor(key, searchQuery, hasFocus, awaitingNavigation);
}
