using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Terminal;

public sealed class WhenUsingThePanelShell
{
    [Fact]
    public void It_offers_explorer_tests_and_changes_with_explorer_selected()
    {
        // Arrange
        var shell = new PanelShell();

        // Act
        var state = shell.State;

        // Assert
        Assert.Equal(
            ("Explorer|Tests|Changes", PanelKind.Explorer),
            (string.Join('|', state.Panels), state.ActivePanel));
    }

    [Fact]
    public void It_changes_the_active_panel_when_tests_is_selected()
    {
        // Arrange
        var shell = new PanelShell();

        // Act
        shell.Select(1);

        // Assert
        Assert.Equal(PanelKind.Tests, shell.State.ActivePanel);
    }

    [Fact]
    public void It_changes_the_active_panel_when_changes_is_selected()
    {
        // Arrange
        var shell = new PanelShell();

        // Act
        shell.Select(2);

        // Assert
        Assert.Equal(PanelKind.Changes, shell.State.ActivePanel);
    }
}
