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
    public void Pressing_l_reruns_the_last_run()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.L));

        // Assert
        Assert.Equal(new TestPanelAction.Dispatch(new ExplorerCommand.RerunLast()), action);
    }

    [Fact]
    public void Pressing_u_reruns_the_failures()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.U));

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
    public void Pressing_n_during_a_search_does_nothing()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.N), searchQuery: "cart");

        // Assert
        Assert.Null(action);
    }

    [Fact]
    public void Pressing_f_moves_to_the_next_failure()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.F));

        // Assert
        Assert.Equal(new TestPanelAction.Dispatch(new ExplorerCommand.NextFailure()), action);
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

    [Fact]
    public void Pressing_capital_E_does_not_open_the_source()
    {
        // Act
        var action = ActionFor(Shifted(KeyCode.E));

        // Assert
        Assert.Null(action);
    }

    [Fact]
    public void Pressing_capital_C_does_not_cancel_the_run()
    {
        // Act
        var action = ActionFor(Shifted(KeyCode.C));

        // Assert
        Assert.Null(action);
    }

    [Fact]
    public void Pressing_capital_R_does_not_run_the_selection()
    {
        // Act
        var action = ActionFor(Shifted(KeyCode.R));

        // Assert
        Assert.Null(action);
    }

    private static Key Shifted(KeyCode keyCode) => new(keyCode | KeyCode.ShiftMask);

    private static TestPanelAction? ActionFor(
        Key key,
        string searchQuery = "",
        bool hasFocus = true) =>
        TestPanelKeyBindings.ActionFor(key, searchQuery, hasFocus);
}
