using Terminal.Gui.Input;
using Terminal.Gui.Drivers;
using TerminalDotnet.Files;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Explorer;

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
    public void Pressing_p_previews_it()
    {
        // Arrange
        var file = new FileEntry("App.csproj", "Order.cs", FileGitStatus.Unchanged);
        var selected = new VisibleFileNode(2, FileNodeKind.File, "Order.cs", [file]);

        // Act
        var action = FilePanelKeyBindings.ActionFor(new Key(KeyCode.P), selected, searchActive: false);

        // Assert
        Assert.Equal(new FilePanelAction.PreviewFile("Order.cs"), action);
    }
}
