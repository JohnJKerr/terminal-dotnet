using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using TerminalDotnet.Files;
using TerminalDotnet.Filters;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Explorer;

public sealed class WhenPressingAKeyInTheFilePanel
{
    [Fact]
    public void Pressing_capital_U_toggles_the_updated_filter()
    {
        // Arrange
        var file = new FileEntry("App.csproj", "Order.cs", FileGitStatus.Unchanged);
        var selected = new VisibleFileNode(2, FileNodeKind.File, "Order.cs", [file]);

        // Act
        var action = FilePanelKeyBindings.ActionFor(new Key(KeyCode.U | KeyCode.ShiftMask), selected, searchActive: false);

        // Assert
        Assert.Equal(new FilePanelAction.ToggleFilter(ExplorerFilter.Updated), action);
    }

    [Fact]
    public void Pressing_capital_A_toggles_every_file()
    {
        // Act
        var action = FilePanelKeyBindings.ActionFor(new Key(KeyCode.A | KeyCode.ShiftMask), selected: null, searchActive: false);

        // Assert
        Assert.Equal(new FilePanelAction.ToggleAllFiles(), action);
    }

    [Fact]
    public void Pressing_capital_U_toggles_the_filter_while_the_filter_hides_every_file()
    {
        // Act
        var action = FilePanelKeyBindings.ActionFor(new Key(KeyCode.U | KeyCode.ShiftMask), selected: null, searchActive: false);

        // Assert
        Assert.Equal(new FilePanelAction.ToggleFilter(ExplorerFilter.Updated), action);
    }

    [Fact]
    public void Pressing_capital_U_while_searching_leaves_the_filter_alone()
    {
        // Arrange
        var file = new FileEntry("App.csproj", "Order.cs", FileGitStatus.Unchanged);
        var selected = new VisibleFileNode(2, FileNodeKind.File, "Order.cs", [file]);

        // Act
        var action = FilePanelKeyBindings.ActionFor(new Key(KeyCode.U | KeyCode.ShiftMask), selected, searchActive: true);

        // Assert
        Assert.Null(action);
    }
}
