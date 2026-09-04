using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using TerminalDotnet.Changes;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Changeset;

public sealed class WhenActingOnAChangedFile
{
    [Fact]
    public void Pressing_enter_shows_its_diff()
    {
        // Arrange
        var selected = Modified();

        // Act
        var action = ChangesetPanelKeyBindings.ActionFor(
            new Key(KeyCode.Enter),
            selected,
            searchActive: false);

        // Assert
        Assert.Equal(new ChangesetAction.ShowDiff(), action);
    }

    [Fact]
    public void Pressing_d_shows_its_diff()
    {
        // Arrange
        var selected = Modified();

        // Act
        var action = ChangesetPanelKeyBindings.ActionFor(
            new Key(KeyCode.D),
            selected,
            searchActive: false);

        // Assert
        Assert.Equal(new ChangesetAction.ShowDiff(), action);
    }

    [Fact]
    public void Pressing_e_opens_it()
    {
        // Arrange
        var selected = Modified();

        // Act
        var action = ChangesetPanelKeyBindings.ActionFor(
            new Key(KeyCode.E),
            selected,
            searchActive: false);

        // Assert
        Assert.Equal(new ChangesetAction.OpenFile("/repo/src/Changed.cs"), action);
    }

    [Fact]
    public void Pressing_p_previews_it()
    {
        // Arrange
        var selected = Modified();

        // Act
        var action = ChangesetPanelKeyBindings.ActionFor(
            new Key(KeyCode.P),
            selected,
            searchActive: false);

        // Assert
        Assert.Equal(new ChangesetAction.PreviewFile("/repo/src/Changed.cs"), action);
    }

    [Fact]
    public void Pressing_r_restores_a_deleted_file()
    {
        // Arrange
        var selected = Deleted();

        // Act
        var action = ChangesetPanelKeyBindings.ActionFor(
            new Key(KeyCode.R),
            selected,
            searchActive: false);

        // Assert
        Assert.Equal(new ChangesetAction.RestoreFile("/repo/src/Gone.cs"), action);
    }

    [Fact]
    public void Pressing_p_previews_nothing_of_a_deleted_file()
    {
        // Arrange
        var selected = Deleted();

        // Act
        var action = ChangesetPanelKeyBindings.ActionFor(
            new Key(KeyCode.P),
            selected,
            searchActive: false);

        // Assert
        Assert.Null(action);
    }

    [Fact]
    public void Pressing_r_restores_nothing_of_a_file_that_is_still_there()
    {
        // Arrange
        var selected = Modified();

        // Act
        var action = ChangesetPanelKeyBindings.ActionFor(
            new Key(KeyCode.R),
            selected,
            searchActive: false);

        // Assert
        Assert.Null(action);
    }

    private static ChangedFile Modified() =>
        new("/repo/src/Changed.cs", "src/Changed.cs", ChangeKind.Modified);

    private static ChangedFile Deleted() =>
        new("/repo/src/Gone.cs", "src/Gone.cs", ChangeKind.Deleted);
}
