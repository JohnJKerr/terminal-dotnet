using TerminalDotnet.Files;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Explorer;

public sealed class WhenCreatingAFilePanelSnapshot
{
    [Fact]
    public void It_recounts_when_the_visible_files_change()
    {
        // Arrange
        var state = new FileExplorerState([]);
        var file = new FileEntry("App.csproj", "Changed.cs", FileGitStatus.Modified);

        // Act
        var updated = state with
        {
            VisibleNodes = [new VisibleFileNode(0, FileNodeKind.File, "Changed.cs", [file])]
        };
        var snapshot = FilePanelSnapshot.From(updated);

        // Assert
        Assert.Equal(1, snapshot.SearchHitCount);
    }

    [Fact]
    public void It_highlights_modified_files_blue_and_new_files_green()
    {
        // Arrange
        var modified = new FileEntry("App.csproj", "Changed.cs", FileGitStatus.Modified);
        var added = new FileEntry("App.csproj", "Added.cs", FileGitStatus.New);
        var state = new FileExplorerState(
        [
            new VisibleFileNode(0, FileNodeKind.Project, "App", [modified, added]),
            new VisibleFileNode(1, FileNodeKind.Folder, "App", [modified, added]),
            new VisibleFileNode(2, FileNodeKind.File, "Changed.cs", [modified]),
            new VisibleFileNode(2, FileNodeKind.File, "Added.cs", [added])
        ]);

        // Act
        var snapshot = FilePanelSnapshot.From(state);

        // Assert
        Assert.Equal(
            [("    • Changed.cs", RowTone.Modified), ("    • Added.cs", RowTone.New)],
            snapshot.Rows.Skip(2).Select(row => (row.Text, row.Tone)));
    }

    [Fact]
    public void It_counts_the_solution_files_and_their_changes_on_the_status_line()
    {
        // Arrange
        var state = new FileExplorerState([]) { Changes = new FileChangeSummary(12, 3, 2, 1) };

        // Act
        var snapshot = FilePanelSnapshot.From(state);

        // Assert
        Assert.Equal(
            ["12 Files", "3 Added", "2 Edited", "1 Deleted"],
            snapshot.StatusSegments.Select(segment => segment.Text));
    }

    [Fact]
    public void It_counts_a_single_file_in_the_singular()
    {
        // Arrange
        var state = new FileExplorerState([]) { Changes = new FileChangeSummary(1, 1, 1, 1) };

        // Act
        var snapshot = FilePanelSnapshot.From(state);

        // Assert
        Assert.Equal(
            ["1 File", "1 Added", "1 Edited", "1 Deleted"],
            snapshot.StatusSegments.Select(segment => segment.Text));
    }

    [Fact]
    public void It_tones_the_status_line_counts_by_the_change_they_report()
    {
        // Arrange
        var state = new FileExplorerState([]) { Changes = new FileChangeSummary(12, 3, 2, 1) };

        // Act
        var snapshot = FilePanelSnapshot.From(state);

        // Assert
        Assert.Equal(
            [RowTone.Neutral, RowTone.New, RowTone.Modified, RowTone.Deleted],
            snapshot.StatusSegments.Select(segment => segment.Tone));
    }

    [Fact]
    public void It_says_why_the_files_could_not_be_read_after_the_counts()
    {
        // Arrange
        var state = new FileExplorerState([]) { Notice = "Could not read the files: Could not start git." };

        // Act
        var snapshot = FilePanelSnapshot.From(state);

        // Assert
        Assert.Equal(
            ["0 Files", "0 Added", "0 Edited", "0 Deleted", "Could not read the files: Could not start git."],
            snapshot.StatusSegments.Select(segment => segment.Text));
    }
}
