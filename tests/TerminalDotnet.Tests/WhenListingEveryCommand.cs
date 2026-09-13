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
    public void It_offers_a_section_for_every_panel_and_for_the_preview()
    {
        // Act
        var titles = CommandMenu.Sections().Select(section => section.Title);

        // Assert
        Assert.Equal(["Anywhere", "Explorer", "Files", "Tests", "Changes", "Preview"], titles);
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
    public void It_names_the_key_that_opens_it()
    {
        // Act
        var entries = CommandMenu.Sections().SelectMany(section => section.Entries);

        // Assert
        Assert.Contains(entries, entry => entry.Keys == "Ctrl+K");
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
