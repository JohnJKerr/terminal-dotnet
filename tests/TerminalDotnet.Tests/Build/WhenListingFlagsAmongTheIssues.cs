using TerminalDotnet.Comments;
using TerminalDotnet.Flags;
using TerminalDotnet.Issues;
using TerminalDotnet.Tests.Builders;
using Xunit;

namespace TerminalDotnet.Tests.Build;

public sealed class WhenListingFlagsAmongTheIssues
{
    [Fact]
    public async Task It_lists_the_flags_after_the_build_issues()
    {
        // Arrange
        var session = Session();
        await session.LoadAsync("/repo/Shop.slnx");

        // Act
        await session.LoadFlagsAsync("/repo/Shop.slnx");

        // Assert
        Assert.Equal(
            [IssueSeverity.Error, IssueSeverity.Flag],
            session.State.Issues.Select(issue => issue.Severity));
    }

    [Fact]
    public async Task It_keeps_the_flags_when_the_build_is_read_again()
    {
        // Arrange
        var session = Session();
        await session.LoadFlagsAsync("/repo/Shop.slnx");

        // Act
        await session.LoadAsync("/repo/Shop.slnx");

        // Assert
        Assert.Equal(2, session.State.Issues.Count);
    }

    [Fact]
    public async Task It_names_the_flag_by_its_heading()
    {
        // Arrange
        var session = Session();

        // Act
        await session.LoadFlagsAsync("/repo/Shop.slnx");

        // Assert
        Assert.Equal("src/Work.cs:12: TODO finish it", session.State.Issues[0].Details);
    }

    [Fact]
    public async Task It_can_narrow_the_issues_to_the_flags()
    {
        // Arrange
        var session = Session();
        await session.LoadAsync("/repo/Shop.slnx");
        await session.LoadFlagsAsync("/repo/Shop.slnx");

        // Act
        await session.DispatchAsync(new IssueCommand.ToggleFlags());

        // Assert
        Assert.Equal([IssueSeverity.Flag], session.State.Issues.Select(issue => issue.Severity));
    }

    private static IssueSession Session() => GivenA.IssuePanel()
        .WithIssues(new CompilationIssue("/repo/src/Broken.cs", "src/Broken.cs", 7, 3, "CS1002", "; expected", IssueSeverity.Error))
        .WithFlags(new Flag("/repo/src/Work.cs", "src/Work.cs", 12, FlagKind.Todo, "finish it"))
        .Build();
}
