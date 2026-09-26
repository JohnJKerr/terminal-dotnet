using Terminal.Gui.Input;
using Terminal.Gui.Drivers;
using TerminalDotnet.Files;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Keyboard;

public sealed class WhenOpeningAFile
{
    [Fact]
    public void Pressing_enter_opens_it()
    {
        // Arrange
        var file = new FileEntry("App.csproj", "Order.cs", FileGitStatus.Unchanged);
        var selected = new VisibleFileNode(2, FileNodeKind.File, "Order.cs", [file]);

        // Act
        var action = FilePanelKeyBindings.ActionFor(new Key(KeyCode.Enter), selected, searchActive: false);

        // Assert
        Assert.Equal(new FilePanelAction.OpenFile("Order.cs"), action);
    }

    [Fact]
    public void Pressing_e_opens_it()
    {
        // Arrange
        var file = new FileEntry("App.csproj", "Order.cs", FileGitStatus.Unchanged);
        var selected = new VisibleFileNode(2, FileNodeKind.File, "Order.cs", [file]);

        // Act
        var action = FilePanelKeyBindings.ActionFor(new Key(KeyCode.E), selected, searchActive: false);

        // Assert
        Assert.Equal(new FilePanelAction.OpenFile("Order.cs"), action);
    }

    [Fact]
    public void Pressing_p_leaves_the_preview_to_follow_the_selection()
    {
        // Arrange
        var file = new FileEntry("App.csproj", "Order.cs", FileGitStatus.Unchanged);
        var selected = new VisibleFileNode(2, FileNodeKind.File, "Order.cs", [file]);

        // Act
        var action = FilePanelKeyBindings.ActionFor(new Key(KeyCode.P), selected, searchActive: false);

        // Assert
        Assert.Null(action);
    }
}
