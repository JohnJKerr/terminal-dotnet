using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using TerminalDotnet.Flags;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Terminal;

public sealed class WhenPressingAKeyInTheFlagPanel
{
    private static readonly Flag Selected = new("/repo/Work.cs", "Work.cs", 14, FlagKind.Todo, "finish it");

    [Fact]
    public void Pressing_enter_edits_the_flag_location()
    {
        // Act
        var action = FlagPanelKeyBindings.ActionFor(new Key(KeyCode.Enter), Selected, searchActive: false);

        // Assert
        Assert.Equal(new FlagPanelAction.Edit("/repo/Work.cs", 14), action);
    }

    [Fact]
    public void Pressing_p_previews_the_flag_location()
    {
        // Act
        var action = FlagPanelKeyBindings.ActionFor(new Key(KeyCode.P), Selected, searchActive: false);

        // Assert
        Assert.Equal(new FlagPanelAction.Preview("/repo/Work.cs", 14), action);
    }

    [Fact]
    public void Pressing_three_filters_warnings()
    {
        // Act
        var action = FlagPanelKeyBindings.ActionFor(new Key(KeyCode.D3), Selected, searchActive: false);

        // Assert
        Assert.Equal(new FlagPanelAction.ToggleFilter(FlagCategory.Warning), action);
    }
}
