using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests;

public sealed class PanelShellTests
{
    [Fact]
    public void ShellOffersExplorerAndTestsWithExplorerSelected()
    {
        // Arrange
        var shell = new PanelShell();

        // Act
        var state = shell.State;

        // Assert
        Assert.Equal(("Explorer|Tests", PanelKind.Explorer), (string.Join('|', state.Panels), state.ActivePanel));
    }
}
