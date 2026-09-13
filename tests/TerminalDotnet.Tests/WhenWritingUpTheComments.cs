using TerminalDotnet.Comments;
using Xunit;

namespace TerminalDotnet.Tests.Comments;

public sealed class WhenWritingUpTheComments
{
    [Fact]
    public void It_heads_each_note_with_the_file_it_is_against()
    {
        // Act
        var report = CommentReport.From(Comments());

        // Assert
        Assert.Equal(
            "## src/Customer.cs\n\nrename this\n\n## src/Order.cs\n\nneeds a guard\n",
            report);
    }

    [Fact]
    public void It_keeps_every_line_of_a_note_that_runs_on()
    {
        // Act
        var report = CommentReport.From(
            [new FileComment("/repo/src/Order.cs", "src/Order.cs", "needs a guard\nand a test")]);

        // Assert
        Assert.Equal("## src/Order.cs\n\nneeds a guard\nand a test\n", report);
    }

    [Fact]
    public void It_writes_nothing_up_when_nothing_is_commented()
    {
        // Act
        var report = CommentReport.From([]);

        // Assert
        Assert.Equal("", report);
    }

    private static IReadOnlyList<FileComment> Comments() =>
    [
        new("/repo/src/Customer.cs", "src/Customer.cs", "rename this"),
        new("/repo/src/Order.cs", "src/Order.cs", "needs a guard")
    ];
}
