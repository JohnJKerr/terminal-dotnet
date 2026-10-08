using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using TerminalDotnet.Files;
using TerminalDotnet.Filters;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Keyboard;

public sealed class WhenPressingAKeyInTheFilePanel
{
    [Fact]
    public void Pressing_right_bracket_steps_from_the_project_files_to_every_file()
    {
        // Act
        var action = FilePanelKeyBindings.ActionFor(RightBracket, selected: null, searchActive: false);

        // Assert
        Assert.Equal(new FilePanelAction.ShowFiles(AllFiles: true, Filter: null), action);
    }

    [Fact]
    public void Pressing_right_bracket_steps_from_every_file_to_the_updated_files()
    {
        // Act
        var action = FilePanelKeyBindings.ActionFor(
            RightBracket,
            selected: null,
            searchActive: false,
            showsAllFiles: true);

        // Assert
        Assert.Equal(new FilePanelAction.ShowFiles(AllFiles: false, Filter: ExplorerFilter.Updated), action);
    }

    [Fact]
    public void Pressing_right_bracket_steps_from_the_updated_files_back_to_the_project_files()
    {
        // Act
        var action = FilePanelKeyBindings.ActionFor(
            RightBracket,
            selected: null,
            searchActive: false,
            activeFilter: ExplorerFilter.Updated);

        // Assert
        Assert.Equal(new FilePanelAction.ShowFiles(AllFiles: false, Filter: null), action);
    }

    [Fact]
    public void Pressing_left_bracket_steps_from_the_project_files_to_the_updated_files()
    {
        // Act
        var action = FilePanelKeyBindings.ActionFor(LeftBracket, selected: null, searchActive: false);

        // Assert
        Assert.Equal(new FilePanelAction.ShowFiles(AllFiles: false, Filter: ExplorerFilter.Updated), action);
    }

    [Fact]
    public void Pressing_right_bracket_while_searching_leaves_the_files_alone()
    {
        // Act
        var action = FilePanelKeyBindings.ActionFor(RightBracket, selected: null, searchActive: true);

        // Assert
        Assert.Null(action);
    }

    [Fact]
    public void Pressing_capital_U_leaves_the_files_alone()
    {
        // Act
        var action = FilePanelKeyBindings.ActionFor(
            new Key(KeyCode.U | KeyCode.ShiftMask),
            selected: null,
            searchActive: false);

        // Assert
        Assert.Null(action);
    }

    private static Key RightBracket => new((KeyCode)']');

    private static Key LeftBracket => new((KeyCode)'[');
}
