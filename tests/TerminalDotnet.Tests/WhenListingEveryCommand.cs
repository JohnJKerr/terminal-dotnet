using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Terminal;

public sealed class WhenListingEveryCommand
{
    [Fact]
    public void It_opens_with_the_commands_that_work_anywhere()
    {
        // Act
        var sections = CommandMenu.Sections();

        // Assert
        Assert.Equal("Anywhere", sections[0].Title);
    }

    [Fact]
    public void It_offers_a_section_for_every_panel_the_preview_and_the_diff()
    {
        // Act
        var titles = CommandMenu.Sections().Select(section => section.Title);

        // Assert
        Assert.Equal(
            ["Anywhere", "Explorer", "Files", "Tests", "Issues", "Changes", "Comments", "Flags", "Preview", "Diff"],
            titles);
    }

    [Fact]
    public void It_lists_the_issue_commands_under_issues()
    {
        // Act
        var issues = CommandMenu.Sections().Single(section => section.Title == "Issues");

        // Assert
        Assert.Equal(
            ["↑/k", "↓/j", "Enter/e", "p", "y", "1", "2", "3"],
            issues.Entries.Select(entry => entry.Keys));
    }

    [Fact]
    public void It_lists_the_editing_command_under_the_preview()
    {
        // Act
        var preview = CommandMenu.Sections().Single(section => section.Title == "Preview");

        // Assert
        Assert.Contains(preview.Entries, entry => entry.Description == "edit the file");
    }

    [Fact]
    public void It_lists_the_commenting_command_under_the_preview()
    {
        // Act
        var preview = CommandMenu.Sections().Single(section => section.Title == "Preview");

        // Assert
        Assert.Contains(preview.Entries, entry => entry.Description == "comment on the file");
    }

    [Fact]
    public void It_names_the_key_that_reaches_the_comments()
    {
        // Act
        var anywhere = CommandMenu.Sections().Single(section => section.Title == "Anywhere");

        // Assert
        Assert.Contains(
            anywhere.Entries,
            entry => entry.Keys == "C" && entry.Description == "go to the Comments");
    }

    [Fact]
    public void It_names_the_key_that_reaches_the_changes()
    {
        // Act
        var anywhere = CommandMenu.Sections().Single(section => section.Title == "Anywhere");

        // Assert
        Assert.Contains(
            anywhere.Entries,
            entry => entry.Keys == "G" && entry.Description == "go to the Changes");
    }

    [Fact]
    public void It_names_the_key_that_reaches_the_issues()
    {
        // Act
        var anywhere = CommandMenu.Sections().Single(section => section.Title == "Anywhere");

        // Assert
        Assert.Contains(
            anywhere.Entries,
            entry => entry.Keys == "I" && entry.Description == "go to the Issues");
    }

    [Fact]
    public void It_lists_stepping_to_the_next_row_under_the_preview()
    {
        // Act
        var preview = CommandMenu.Sections().Single(section => section.Title == "Preview");

        // Assert
        Assert.Contains(
            preview.Entries,
            entry => entry.Keys == "n" && entry.Description == "preview the next row of the panel");
    }

    [Fact]
    public void It_lists_stepping_to_the_next_file_under_the_diff()
    {
        // Act
        var diff = CommandMenu.Sections().Single(section => section.Title == "Diff");

        // Assert
        Assert.Contains(
            diff.Entries,
            entry => entry.Keys == "n" && entry.Description == "show the next file's diff");
    }

    [Fact]
    public void It_lists_commenting_under_the_diff()
    {
        // Act
        var diff = CommandMenu.Sections().Single(section => section.Title == "Diff");

        // Assert
        Assert.Contains(
            diff.Entries,
            entry => entry.Keys == "c" && entry.Description == "comment on the file");
    }

    [Fact]
    public void It_names_the_key_that_opens_it()
    {
        // Act
        var entries = CommandMenu.Sections().SelectMany(section => section.Entries);

        // Assert
        Assert.Contains(entries, entry => entry.Keys == "?");
    }

    [Fact]
    public void It_says_the_global_refresh_also_rebuilds()
    {
        // Act
        var anywhere = CommandMenu.Sections().Single(section => section.Title == "Anywhere");

        // Assert
        Assert.Contains(
            anywhere.Entries,
            entry => entry.Keys == "Ctrl+R" && entry.Description == "refresh and rebuild the workspace");
    }

    [Fact]
    public void It_lists_the_run_commands_under_tests()
    {
        // Act
        var tests = CommandMenu.Sections().Single(section => section.Title == "Tests");

        // Assert
        Assert.Contains(tests.Entries, entry => entry.Description == "run the selection");
    }

    [Fact]
    public void It_heads_each_section_with_its_title()
    {
        // Act
        var rows = CommandMenu.Rows();

        // Assert
        Assert.Contains(rows, row => row.IsHeading && row.Text == "Explorer");
    }

    [Fact]
    public void It_names_the_key_that_reaches_the_files()
    {
        // Act
        var anywhere = CommandMenu.Sections().Single(section => section.Title == "Anywhere");

        // Assert
        Assert.Contains(anywhere.Entries, entry => entry.Keys == "F");
    }

    [Fact]
    public void It_lists_the_file_commands_under_the_files()
    {
        // Act
        var files = CommandMenu.Sections().Single(section => section.Title == "Files");

        // Assert
        Assert.Contains(files.Entries, entry => entry.Description == "edit the file");
    }

    [Fact]
    public void It_separates_each_section_with_a_blank_line()
    {
        // Act
        var rows = CommandMenu.Rows();

        // Assert
        Assert.Equal(CommandMenu.Sections().Count - 1, rows.Count(row => row.Text.Length == 0));
    }

    [Fact]
    public void It_aligns_every_description_in_one_column()
    {
        // Arrange
        var descriptions = CommandMenu.Sections()
            .SelectMany(section => section.Entries)
            .Select(entry => entry.Description)
            .ToArray();

        // Act
        var columns = CommandMenu.Rows()
            .Select(row => (row.Text, Description: LongestSuffix(row.Text, descriptions)))
            .Where(entry => entry.Description is not null)
            .Select(entry => entry.Text.Length - entry.Description!.Length)
            .ToHashSet();

        // Assert
        Assert.Single(columns);
    }

    private static string? LongestSuffix(string row, IReadOnlyList<string> descriptions) => descriptions
        .Where(description => row.EndsWith(description, StringComparison.Ordinal))
        .OrderByDescending(description => description.Length)
        .FirstOrDefault();
}
