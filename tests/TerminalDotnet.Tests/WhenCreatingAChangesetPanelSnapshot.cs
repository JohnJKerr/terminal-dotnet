using TerminalDotnet.Changes;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Changeset;

public sealed class WhenCreatingAChangesetPanelSnapshot
{
    [Fact]
    public void It_marks_each_row_with_the_change_it_reports()
    {
        // Arrange
        var state = new ChangesetState(ChangedFiles());

        // Act
        var snapshot = ChangesetPanelSnapshot.From(state);

        // Assert
        Assert.Equal(
            ["+ src/Added.cs", "~ src/Changed.cs", "- src/Gone.cs"],
            snapshot.Rows.Select(row => row.Text));
    }

    [Fact]
    public void It_tones_each_row_by_the_change_it_reports()
    {
        // Arrange
        var state = new ChangesetState(ChangedFiles());

        // Act
        var snapshot = ChangesetPanelSnapshot.From(state);

        // Assert
        Assert.Equal(
            [FileRowTone.New, FileRowTone.Modified, FileRowTone.Deleted],
            snapshot.Rows.Select(row => row.Tone));
    }

    [Fact]
    public void It_counts_the_changed_added_and_deleted_files_on_the_status_line()
    {
        // Arrange
        var state = new ChangesetState([]) { Summary = new ChangesetSummary(4, 2, 1) };

        // Act
        var snapshot = ChangesetPanelSnapshot.From(state);

        // Assert
        Assert.Equal(
            ["4 Changed", "2 Added", "1 Deleted"],
            snapshot.StatusSegments.Select(segment => segment.Text));
    }

    [Fact]
    public void It_tones_the_status_line_counts_by_the_change_they_report()
    {
        // Arrange
        var state = new ChangesetState([]) { Summary = new ChangesetSummary(4, 2, 1) };

        // Act
        var snapshot = ChangesetPanelSnapshot.From(state);

        // Assert
        Assert.Equal(
            [FileRowTone.Modified, FileRowTone.New, FileRowTone.Deleted],
            snapshot.StatusSegments.Select(segment => segment.Tone));
    }

    [Fact]
    public void It_titles_the_diff_with_the_file_it_describes()
    {
        // Arrange
        var state = new ChangesetState([])
        {
            Diff = new DiffContext("src/Changed.cs", "@@ -1 +1 @@")
        };

        // Act
        var snapshot = ChangesetPanelSnapshot.From(state);

        // Assert
        Assert.Equal("src/Changed.cs", snapshot.DiffTitle);
    }

    [Fact]
    public void It_reads_no_diff_lines_before_a_diff_is_loaded()
    {
        // Arrange
        var state = new ChangesetState(ChangedFiles());

        // Act
        var snapshot = ChangesetPanelSnapshot.From(state);

        // Assert
        Assert.Empty(snapshot.DiffLines);
    }

    private static IReadOnlyList<ChangedFile> ChangedFiles() =>
    [
        new("/repo/src/Added.cs", "src/Added.cs", ChangeKind.Added),
        new("/repo/src/Changed.cs", "src/Changed.cs", ChangeKind.Modified),
        new("/repo/src/Gone.cs", "src/Gone.cs", ChangeKind.Deleted)
    ];
}
