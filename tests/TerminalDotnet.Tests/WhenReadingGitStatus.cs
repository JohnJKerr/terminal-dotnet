using TerminalDotnet.Git;
using Xunit;

namespace TerminalDotnet.Tests.Changeset;

public sealed class WhenReadingGitStatus
{
    [Fact]
    public void It_reads_the_path_and_kind_of_every_change()
    {
        // Act
        var entries = GitStatusOutput.EntriesFrom("?? src/Added.cs\0 M src/Changed.cs\0 D src/Gone.cs\0");

        // Assert
        Assert.Equal(
            [
                ("src/Added.cs", GitChangeKind.Added),
                ("src/Changed.cs", GitChangeKind.Modified),
                ("src/Gone.cs", GitChangeKind.Deleted)
            ],
            entries.Select(entry => (entry.RelativePath, entry.Kind)));
    }

    [Fact]
    public void It_reads_a_renamed_file_at_its_new_path()
    {
        // Act
        var entries = GitStatusOutput.EntriesFrom("R  src/New.cs\0src/Old.cs\0");

        // Assert
        Assert.Equal("src/New.cs", entries.Single().RelativePath);
    }

    [Fact]
    public void It_reads_the_change_staged_in_the_index()
    {
        // Act
        var entries = GitStatusOutput.EntriesFrom("MD src/Gone.cs\0");

        // Assert
        Assert.Equal(GitChangeKind.Modified, entries.Single().Staged);
    }

    [Fact]
    public void It_reads_the_change_left_in_the_working_tree()
    {
        // Act
        var entries = GitStatusOutput.EntriesFrom("MD src/Gone.cs\0");

        // Assert
        Assert.Equal(GitChangeKind.Deleted, entries.Single().Unstaged);
    }

    [Fact]
    public void It_reads_a_staged_addition_as_added()
    {
        // Act
        var entries = GitStatusOutput.EntriesFrom("A  src/Added.cs\0");

        // Assert
        Assert.Equal(GitChangeKind.Added, entries.Single().Kind);
    }

    [Fact]
    public void It_skips_lines_too_short_to_name_a_file()
    {
        // Act
        var entries = GitStatusOutput.EntriesFrom(" M \0 M src/Changed.cs\0");

        // Assert
        Assert.Equal(["src/Changed.cs"], entries.Select(entry => entry.RelativePath));
    }

    [Fact]
    public void It_reads_a_path_holding_an_arrow_as_one_file()
    {
        // Act
        var entries = GitStatusOutput.EntriesFrom(" M src/a -> b.cs\0");

        // Assert
        Assert.Equal("src/a -> b.cs", entries.Single().RelativePath);
    }

    [Fact]
    public void It_reads_a_path_holding_a_newline_as_one_file()
    {
        // Act
        var entries = GitStatusOutput.EntriesFrom(" M src/two\nlines.cs\0");

        // Assert
        Assert.Equal("src/two\nlines.cs", entries.Single().RelativePath);
    }

    [Fact]
    public void It_reads_nothing_from_a_clean_repository()
    {
        // Act
        var entries = GitStatusOutput.EntriesFrom("");

        // Assert
        Assert.Empty(entries);
    }
}
