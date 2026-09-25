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
            ["Preview", "Explorer", "Tests", "Changes", "Issues", "Comments"],
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
        shell.Select(PanelKind.Tests);

        // Assert
        Assert.Equal(PanelKind.Tests, shell.State.ActivePanel);
    }

    [Fact]
    public void It_changes_the_active_panel_when_issues_is_selected()
    {
        // Arrange
        var shell = new PanelShell();

        // Act
        shell.Select(PanelKind.Issues);

        // Assert
        Assert.Equal(PanelKind.Issues, shell.State.ActivePanel);
    }

    [Fact]
    public void It_changes_the_active_panel_when_changes_is_selected()
    {
        // Arrange
        var shell = new PanelShell();

        // Act
        shell.Select(PanelKind.Changes);

        // Assert
        Assert.Equal(PanelKind.Changes, shell.State.ActivePanel);
    }

    [Fact]
    public void It_changes_the_active_panel_when_the_comments_are_selected()
    {
        // Arrange
        var shell = new PanelShell();

        // Act
        shell.Select(PanelKind.Comments);

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
        shell.Select(PanelKind.Tests);

        // Assert
        Assert.Equal(PanelKind.Tests, shell.State.ExpandedList);
    }

    [Fact]
    public void It_keeps_the_last_list_expanded_while_the_reader_is_in_the_issues()
    {
        // Arrange
        var shell = new PanelShell();
        shell.Select(PanelKind.Changes);

        // Act
        shell.Select(PanelKind.Issues);

        // Assert
        Assert.Equal(PanelKind.Changes, shell.State.ExpandedList);
    }

    [Fact]
    public void It_previews_the_explorer_at_first()
    {
        // Arrange
        var shell = new PanelShell();

        // Act
        var state = shell.State;

        // Assert
        Assert.Equal(PanelKind.Explorer, state.PreviewedList);
    }

    [Fact]
    public void It_previews_the_list_the_reader_moves_to()
    {
        // Arrange
        var shell = new PanelShell();

        // Act
        shell.Select(PanelKind.Issues);

        // Assert
        Assert.Equal(PanelKind.Issues, shell.State.PreviewedList);
    }

    [Fact]
    public void It_keeps_previewing_the_last_list_from_inside_the_preview()
    {
        // Arrange
        var shell = new PanelShell();
        shell.Select(PanelKind.Tests);

        // Act
        shell.Select(PanelKind.Preview);

        // Assert
        Assert.Equal(PanelKind.Tests, shell.State.PreviewedList);
    }

    [Fact]
    public void It_previews_a_change_as_its_diff_at_first()
    {
        // Arrange
        var shell = new PanelShell();

        // Act
        var state = shell.State;

        // Assert
        Assert.False(state.PreviewsChangedFile);
    }

    [Fact]
    public void It_previews_the_changed_file_once_asked_to()
    {
        // Arrange
        var shell = new PanelShell();

        // Act
        shell.PreviewChangedFile();

        // Assert
        Assert.True(shell.State.PreviewsChangedFile);
    }

    [Fact]
    public void It_returns_to_the_diff_once_asked_to()
    {
        // Arrange
        var shell = new PanelShell();
        shell.PreviewChangedFile();

        // Act
        shell.PreviewChangeDiff();

        // Assert
        Assert.False(shell.State.PreviewsChangedFile);
    }

    [Fact]
    public void It_shows_the_active_panel_full_screen_when_asked()
    {
        // Arrange
        var shell = new PanelShell();
        shell.Select(PanelKind.Tests);

        // Act
        shell.ToggleFullScreen();

        // Assert
        Assert.Equal(PanelKind.Tests, shell.State.FullScreenPanel);
    }

    [Fact]
    public void It_returns_to_the_tiles_when_full_screen_is_asked_for_again()
    {
        // Arrange
        var shell = new PanelShell();
        shell.ToggleFullScreen();

        // Act
        shell.ToggleFullScreen();

        // Assert
        Assert.Null(shell.State.FullScreenPanel);
    }

    [Fact]
    public void It_keeps_full_screen_on_the_panel_moved_to()
    {
        // Arrange
        var shell = new PanelShell();
        shell.ToggleFullScreen();

        // Act
        shell.SelectNext();

        // Assert
        Assert.Equal(PanelKind.Tests, shell.State.FullScreenPanel);
    }

    [Fact]
    public void It_returns_to_the_tiles_when_full_screen_is_left()
    {
        // Arrange
        var shell = new PanelShell();
        shell.ToggleFullScreen();

        // Act
        shell.LeaveFullScreen();

        // Assert
        Assert.Null(shell.State.FullScreenPanel);
    }

    [Fact]
    public void Leaving_full_screen_reports_that_it_was_left()
    {
        // Arrange
        var shell = new PanelShell();
        shell.ToggleFullScreen();

        // Act
        var left = shell.LeaveFullScreen();

        // Assert
        Assert.True(left);
    }

    [Fact]
    public void Leaving_full_screen_does_nothing_on_the_tiles()
    {
        // Arrange
        var shell = new PanelShell();

        // Act
        var left = shell.LeaveFullScreen();

        // Assert
        Assert.False(left);
    }
}
