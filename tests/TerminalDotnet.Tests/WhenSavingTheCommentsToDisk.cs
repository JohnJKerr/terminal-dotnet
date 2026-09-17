using TerminalDotnet.Comments;
using Xunit;

namespace TerminalDotnet.Tests.Comments;

public sealed class WhenSavingTheCommentsToDisk
{
    [Fact]
    public async Task It_leaves_alone_the_file_a_symbolic_link_points_at()
    {
        // Arrange
        var root = Path.Combine(Path.GetTempPath(), $"terminal-dotnet-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(root);
            var elsewhere = Path.Combine(root, "elsewhere.txt");
            await File.WriteAllTextAsync(elsewhere, "keep me");
            var planted = Path.Combine(root, "comments.md");
            File.CreateSymbolicLink(planted, elsewhere);

            // Act
            await new FileCommentStore().TryWriteAsync(planted, "the notes");

            // Assert
            Assert.Equal("keep me", await File.ReadAllTextAsync(elsewhere));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
