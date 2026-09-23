using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Terminal;

public sealed class WhenUsingThePanelShell
{
    [Fact]
    public void It_offers_every_panel()
    {
        // Arrange
        var shell = new PanelShell();

        // Act
        var state = shell.State;

        // Assert
        Assert.Equal(
            ["Explorer", "Tests", "Changes", "Issues", "Comments"],
            state.Panels);
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
    public void It_changes_the_active_panel_when_issues_is_selected()
    {
        // Arrange
        var shell = new PanelShell();

        // Act
        shell.Select(3);

        // Assert
        Assert.Equal(PanelKind.Issues, shell.State.ActivePanel);
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

    [Fact]
    public void It_changes_the_active_panel_when_the_comments_are_selected()
    {
        // Arrange
        var shell = new PanelShell();

        // Act
        shell.Select(4);

        // Assert
        Assert.Equal(PanelKind.Comments, shell.State.ActivePanel);
    }

    [Fact]
    public void It_starts_the_explorer_on_the_project_files()
    {
        // Arrange
        var shell = new PanelShell();

        // Act
        var state = shell.State;

        // Assert
        Assert.False(state.ShowsAllFiles);
    }

    [Fact]
    public void It_shows_every_file_in_the_explorer_once_asked_to()
    {
        // Arrange
        var shell = new PanelShell();

        // Act
        shell.ToggleAllFiles();

        // Assert
        Assert.True(shell.State.ShowsAllFiles);
    }

    [Fact]
    public void It_returns_the_explorer_to_the_project_files_when_asked_again()
    {
        // Arrange
        var shell = new PanelShell();
        shell.ToggleAllFiles();

        // Act
        shell.ToggleAllFiles();

        // Assert
        Assert.False(shell.State.ShowsAllFiles);
    }

    [Fact]
    public void It_expands_the_explorer_at_first()
    {
        // Arrange
        var shell = new PanelShell();

        // Act
        var state = shell.State;

        // Assert
        Assert.Equal(PanelKind.Explorer, state.ExpandedList);
    }

    [Fact]
    public void It_expands_the_list_the_reader_moves_to()
    {
        // Arrange
        var shell = new PanelShell();

        // Act
        shell.Select((int)PanelKind.Tests);

        // Assert
        Assert.Equal(PanelKind.Tests, shell.State.ExpandedList);
    }

    [Fact]
    public void It_keeps_the_last_list_expanded_while_the_reader_is_in_the_issues()
    {
        // Arrange
        var shell = new PanelShell();
        shell.Select((int)PanelKind.Changes);

        // Act
        shell.Select((int)PanelKind.Issues);

        // Assert
        Assert.Equal(PanelKind.Changes, shell.State.ExpandedList);
    }
}
