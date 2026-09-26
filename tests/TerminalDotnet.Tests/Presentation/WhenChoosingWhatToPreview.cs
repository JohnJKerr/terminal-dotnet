using TerminalDotnet.Changes;
using TerminalDotnet.Comments;
using TerminalDotnet.Explorer;
using TerminalDotnet.Files;
using TerminalDotnet.Issues;
using TerminalDotnet.Terminal;
using TerminalDotnet.Testing;
using Xunit;

namespace TerminalDotnet.Tests.Presentation;

public sealed class WhenChoosingWhatToPreview
{
    [Fact]
    public void It_previews_the_file_selected_in_the_explorer()
    {
        // Arrange
        var files = new FileExplorerState([new VisibleFileNode(1, FileNodeKind.File, "Order.cs", [File()])]);

        // Act
        var subject = PreviewSubject.For(PanelKind.Explorer, Panels() with { Files = files });

        // Assert
        Assert.Equal(new PreviewSubject.SourceFile("/repo/Order.cs", 1), subject);
    }

    [Fact]
    public void It_previews_nothing_for_a_folder()
    {
        // Arrange
        var files = new FileExplorerState([new VisibleFileNode(0, FileNodeKind.Folder, "src", [File()])]);

        // Act
        var subject = PreviewSubject.For(PanelKind.Explorer, Panels() with { Files = files });

        // Assert
        Assert.Equal(new PreviewSubject.Nothing(), subject);
    }

    [Fact]
    public void It_previews_nothing_for_an_empty_list()
    {
        // Act
        var subject = PreviewSubject.For(PanelKind.Comments, Panels());

        // Assert
        Assert.Equal(new PreviewSubject.Nothing(), subject);
    }

    [Fact]
    public void It_previews_the_diff_of_the_selected_change()
    {
        // Arrange
        var changes = new ChangesetState([new ChangedFile("/repo/Order.cs", "Order.cs", ChangeKind.Modified)]);

        // Act
        var subject = PreviewSubject.For(PanelKind.Changes, Panels() with { Changes = changes });

        // Assert
        Assert.Equal(new PreviewSubject.ChangeDiff("/repo/Order.cs"), subject);
    }

    [Fact]
    public void It_previews_the_changed_file_when_asked_to()
    {
        // Arrange
        var changes = new ChangesetState([new ChangedFile("/repo/Order.cs", "Order.cs", ChangeKind.Modified)]);

        // Act
        var subject = PreviewSubject.For(PanelKind.Changes, Panels() with { Changes = changes }, previewsChangedFile: true);

        // Assert
        Assert.Equal(new PreviewSubject.SourceFile("/repo/Order.cs", 1), subject);
    }

    [Fact]
    public void It_previews_a_deleted_file_as_its_diff_even_when_asked_for_the_file()
    {
        // Arrange
        var changes = new ChangesetState([new ChangedFile("/repo/Order.cs", "Order.cs", ChangeKind.Deleted)]);

        // Act
        var subject = PreviewSubject.For(PanelKind.Changes, Panels() with { Changes = changes }, previewsChangedFile: true);

        // Assert
        Assert.Equal(new PreviewSubject.ChangeDiff("/repo/Order.cs"), subject);
    }

    [Fact]
    public void It_previews_the_line_an_issue_reports()
    {
        // Arrange
        var issues = new IssueState([
            new CompilationIssue("/repo/Order.cs", "Order.cs", 12, 5, "CS0103", "missing", IssueSeverity.Error)
        ]);

        // Act
        var subject = PreviewSubject.For(PanelKind.Issues, Panels() with { Issues = issues });

        // Assert
        Assert.Equal(new PreviewSubject.SourceFile("/repo/Order.cs", 12), subject);
    }

    [Fact]
    public void It_previews_the_file_a_comment_is_against()
    {
        // Arrange
        var comments = new CommentsState([new FileComment("/repo/Order.cs", "Order.cs", "needs a guard")]);

        // Act
        var subject = PreviewSubject.For(PanelKind.Comments, Panels() with { Comments = comments });

        // Assert
        Assert.Equal(new PreviewSubject.SourceFile("/repo/Order.cs", 1), subject);
    }

    [Fact]
    public void It_asks_for_the_source_of_the_selected_test()
    {
        // Arrange
        var node = new VisibleTestNode(2, TestNodeKind.Test, "Adds item", [Test()]);
        var tests = new ExplorerState(ExplorerStatus.Ready, [node], 0, "Ready");

        // Act
        var subject = PreviewSubject.For(PanelKind.Tests, Panels() with { Tests = tests });

        // Assert
        Assert.Equal(new PreviewSubject.SelectedTest(node), subject);
    }

    private static PanelStates Panels() => new(
        new FileExplorerState([]),
        new ExplorerState(ExplorerStatus.Ready, [], 0, "Ready"),
        new ChangesetState([]),
        new IssueState([]),
        new CommentsState([]));

    private static FileEntry File() => new("/repo/App.csproj", "/repo/Order.cs", FileGitStatus.Unchanged);

    private static TestCase Test() => new("Shop.Tests.CartTests.Adds_item", "Adds item", "Shop.Tests.csproj");
}
