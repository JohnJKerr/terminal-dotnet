using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Terminal;

public sealed class WhenUsingThePanelShell
{
    [Fact]
    public void It_offers_the_explorer_the_files_the_tests_and_the_changes()
    {
        // Arrange
        var shell = new PanelShell();

        // Act
        var state = shell.State;

        // Assert
        Assert.Equal(["Explorer", "Files", "Tests", "Changes"], state.Panels);
    }

    [Fact]
    public void It_starts_on_the_explorer()
    {
        // Arrange
        var shell = new PanelShell();

        // Act
        var state = shell.State;

        // Assert
        Assert.Equal(PanelKind.Explorer, state.ActivePanel);
    }

    [Fact]
    public void It_changes_the_active_panel_when_the_files_are_selected()
    {
        // Arrange
        var shell = new PanelShell();

        // Act
        shell.Select(1);

        // Assert
        Assert.Equal(PanelKind.Files, shell.State.ActivePanel);
    }

    [Fact]
    public void It_changes_the_active_panel_when_tests_is_selected()
    {
        // Arrange
        var shell = new PanelShell();

        // Act
        shell.Select(2);

        // Assert
        Assert.Equal(PanelKind.Tests, shell.State.ActivePanel);
    }

    [Fact]
    public void It_changes_the_active_panel_when_changes_is_selected()
    {
        // Arrange
        var shell = new PanelShell();

        // Act
        shell.Select(3);

        // Assert
        Assert.Equal(PanelKind.Changes, shell.State.ActivePanel);
    }

    [Fact]
    public void It_labels_each_panel_with_the_key_that_reaches_it()
    {
        // Arrange
        var shell = new PanelShell();

        // Act
        var panels = shell.State.KeyedPanels;

        // Assert
        Assert.Equal(["E", "F", "T", "C"], panels.Select(panel => panel.Key));
    }

    [Fact]
    public void It_names_each_panel_beside_its_key()
    {
        // Arrange
        var shell = new PanelShell();

        // Act
        var panels = shell.State.KeyedPanels;

        // Assert
        Assert.Equal(
            ["Explorer", "Files", "Tests", "Changes"],
            panels.Select(panel => panel.Name));
    }
}
