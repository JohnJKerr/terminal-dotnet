using TerminalDotnet.Comments;
using TerminalDotnet.Tests.Builders;
using Xunit;

namespace TerminalDotnet.Tests.Comments;

public sealed class WhenSavingTheCommentsToDisk
{
    [Fact]
    public async Task It_leaves_alone_the_file_a_symbolic_link_points_at()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create().WithFile("elsewhere.txt", "keep me");
        File.CreateSymbolicLink(workspace.PathTo("comments.md"), workspace.PathTo("elsewhere.txt"));

        // Act
        await new FileCommentStore().TryWriteAsync(workspace.PathTo("comments.md"), "the notes");

        // Assert
        Assert.Equal("keep me", await File.ReadAllTextAsync(workspace.PathTo("elsewhere.txt")));
    }
}
