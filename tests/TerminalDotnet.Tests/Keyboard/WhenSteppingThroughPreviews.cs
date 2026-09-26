using TerminalDotnet.Changes;
using TerminalDotnet.Comments;
using TerminalDotnet.Explorer;
using TerminalDotnet.Files;
using TerminalDotnet.Issues;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Keyboard;

/// <summary>A folder or project has nothing to preview, so stepping from the
/// preview goes straight past it to the next row that does.</summary>
public sealed class WhenSteppingThroughPreviews
{
    [Fact]
    public void Stepping_forward_passes_over_a_folder_to_the_next_file()
    {
        // Arrange
        var panels = Explorer(selected: 0, File("Order.cs"), Folder("Tests"), File("Customer.cs"));

        // Act
        var next = PreviewSubject.NextShown(PanelKind.Explorer, panels, step: 1);

        // Assert
        Assert.Equal(2, next);
    }

    [Fact]
    public void Stepping_back_passes_over_a_folder_to_the_previous_file()
    {
        // Arrange
        var panels = Explorer(selected: 2, File("Order.cs"), Folder("Tests"), File("Customer.cs"));

        // Act
        var next = PreviewSubject.NextShown(PanelKind.Explorer, panels, step: -1);

        // Assert
        Assert.Equal(0, next);
    }

    [Fact]
    public void Stepping_moves_to_the_very_next_row_when_it_has_a_preview()
    {
        // Arrange
        var panels = Explorer(selected: 0, File("Order.cs"), File("Customer.cs"));

        // Act
        var next = PreviewSubject.NextShown(PanelKind.Explorer, panels, step: 1);

        // Assert
        Assert.Equal(1, next);
    }

    [Fact]
    public void Stepping_leaves_a_folder_it_started_on()
    {
        // Arrange
        var panels = Explorer(selected: 0, Folder("src"), Folder("Tests"), File("Order.cs"));

        // Act
        var next = PreviewSubject.NextShown(PanelKind.Explorer, panels, step: 1);

        // Assert
        Assert.Equal(2, next);
    }

    [Fact]
    public void Stepping_stays_put_when_only_folders_lie_ahead()
    {
        // Arrange
        var panels = Explorer(selected: 0, File("Order.cs"), Folder("Tests"), Folder("docs"));

        // Act
        var next = PreviewSubject.NextShown(PanelKind.Explorer, panels, step: 1);

        // Assert
        Assert.Null(next);
    }

    [Fact]
    public void Stepping_stays_put_at_the_end_of_the_list()
    {
        // Arrange
        var panels = Explorer(selected: 1, File("Order.cs"), File("Customer.cs"));

        // Act
        var next = PreviewSubject.NextShown(PanelKind.Explorer, panels, step: 1);

        // Assert
        Assert.Null(next);
    }

    [Fact]
    public void Stepping_through_the_changes_moves_to_the_next_change()
    {
        // Arrange
        var changes = new ChangesetState(
        [
            new ChangedFile("/repo/Order.cs", "Order.cs", ChangeKind.Modified),
            new ChangedFile("/repo/Customer.cs", "Customer.cs", ChangeKind.Added)
        ]);

        // Act
        var next = PreviewSubject.NextShown(PanelKind.Changes, Panels() with { Changes = changes }, step: 1);

        // Assert
        Assert.Equal(1, next);
    }

    private static PanelStates Explorer(int selected, params VisibleFileNode[] nodes) =>
        Panels() with { Files = new FileExplorerState(nodes, selected) };

    private static VisibleFileNode File(string name) =>
        new(1, FileNodeKind.File, name, [new FileEntry("/repo/App.csproj", $"/repo/{name}", FileGitStatus.Unchanged)]);

    private static VisibleFileNode Folder(string name) => new(0, FileNodeKind.Folder, name, []);

    private static PanelStates Panels() => new(
        new FileExplorerState([]),
        new ExplorerState(ExplorerStatus.Ready, [], 0, "Ready"),
        new ChangesetState([]),
        new IssueState([]),
        new CommentsState([]));
}
