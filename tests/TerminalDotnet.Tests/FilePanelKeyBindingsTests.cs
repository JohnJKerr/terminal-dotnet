using Terminal.Gui.Input;
using Terminal.Gui.Drivers;
using TerminalDotnet.Files;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests;

public sealed class FilePanelKeyBindingsTests
{
    [Theory]
    [InlineData(KeyCode.Enter)]
    [InlineData(KeyCode.O)]
    public void FileActivationKeysOpenTheSelectedFile(KeyCode keyCode)
    {
        // Arrange
        var file = new FileEntry("App.csproj", "App", "Order.cs", FileGitStatus.Unchanged);
        var selected = new VisibleFileNode(2, FileNodeKind.File, "Order.cs", [file]);

        // Act
        var action = FilePanelKeyBindings.ActionFor(new Key(keyCode), selected, searchActive: false);

        // Assert
        Assert.Equal(new FilePanelAction.OpenFile("Order.cs"), action);
    }
}
