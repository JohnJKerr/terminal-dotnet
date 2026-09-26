using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Build;

/// <summary>The tests and the issues describe the last build, so opening either
/// rebuilds when the tree has been edited since, and only then: flicking
/// between panels with nothing changed must not set a build off each time.
/// </summary>
public sealed class WhenTheBuildGoesStale
{
    [Fact]
    public void Opening_the_tests_does_not_rebuild_when_nothing_was_edited()
    {
        // Arrange
        var edits = new EditsSinceTheBuild();

        // Act
        var worth = edits.WorthRebuildingOnOpening(PanelKind.Explorer, PanelKind.Tests);

        // Assert
        Assert.False(worth);
    }

    [Fact]
    public void Opening_the_tests_rebuilds_once_a_file_was_edited()
    {
        // Arrange
        var edits = new EditsSinceTheBuild();
        edits.Noticed("src/Order.cs");

        // Act
        var worth = edits.WorthRebuildingOnOpening(PanelKind.Explorer, PanelKind.Tests);

        // Assert
        Assert.True(worth);
    }

    [Fact]
    public void Opening_the_issues_rebuilds_too()
    {
        // Arrange
        var edits = new EditsSinceTheBuild();
        edits.Noticed("src/Order.cs");

        // Act
        var worth = edits.WorthRebuildingOnOpening(PanelKind.Changes, PanelKind.Issues);

        // Assert
        Assert.True(worth);
    }

    [Fact]
    public void Opening_any_other_panel_does_not()
    {
        // Arrange
        var edits = new EditsSinceTheBuild();
        edits.Noticed("src/Order.cs");

        // Act
        var worth = edits.WorthRebuildingOnOpening(PanelKind.Tests, PanelKind.Changes);

        // Assert
        Assert.False(worth);
    }

    [Fact]
    public void Choosing_the_panel_the_reader_is_already_on_does_not()
    {
        // Arrange
        var edits = new EditsSinceTheBuild();
        edits.Noticed("src/Order.cs");

        // Act
        var worth = edits.WorthRebuildingOnOpening(PanelKind.Tests, PanelKind.Tests);

        // Assert
        Assert.False(worth);
    }

    [Fact]
    public void Build_output_does_not_make_the_build_stale()
    {
        // Arrange
        var edits = new EditsSinceTheBuild();
        edits.Noticed("src/obj/Debug/Order.g.cs");

        // Act
        var worth = edits.WorthRebuildingOnOpening(PanelKind.Explorer, PanelKind.Tests);

        // Assert
        Assert.False(worth);
    }

    [Fact]
    public void A_save_from_the_editor_makes_it_stale()
    {
        // Arrange
        var edits = new EditsSinceTheBuild();
        edits.Noticed();

        // Act
        var worth = edits.WorthRebuildingOnOpening(PanelKind.Explorer, PanelKind.Issues);

        // Assert
        Assert.True(worth);
    }

    [Fact]
    public void Starting_a_rebuild_answers_the_edits_made_before_it()
    {
        // Arrange
        var edits = new EditsSinceTheBuild();
        edits.Noticed("src/Order.cs");
        edits.Built();

        // Act
        var worth = edits.WorthRebuildingOnOpening(PanelKind.Explorer, PanelKind.Tests);

        // Assert
        Assert.False(worth);
    }

    [Fact]
    public void An_edit_made_while_it_builds_is_kept_for_the_next_build()
    {
        // Arrange
        var edits = new EditsSinceTheBuild();
        edits.Noticed("src/Order.cs");
        edits.Built();
        edits.Noticed("src/Basket.cs");

        // Act
        var worth = edits.WorthRebuildingOnOpening(PanelKind.Explorer, PanelKind.Tests);

        // Assert
        Assert.True(worth);
    }
}
