using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Terminal;

public sealed class WhenActingOnAPreviewedFile
{
    [Fact]
    public void Pressing_e_opens_the_file_in_the_editor()
    {
        // Act
        var action = PreviewKeyBindings.ActionFor(new Key(KeyCode.E), viewportHeight: 20);

        // Assert
        Assert.Equal(new PreviewAction.Edit(), action);
    }
}
