using TerminalDotnet.Comments;
using TerminalDotnet.Tests.Builders;
using TerminalDotnet.Tests.Fakes;
using Xunit;

namespace TerminalDotnet.Tests.Comments;

public sealed class WhenSavingOverAnExistingFile
{
    [Fact]
    public async Task It_says_a_path_that_already_holds_something_does()
    {
        // Arrange
        var session = GivenA.CommentSession().WithStore(new RecordingCommentStore("comments.md")).Build();

        // Act
        var holds = await session.HoldsSomethingAtAsync("comments.md");

        // Assert
        Assert.True(holds);
    }

    [Fact]
    public async Task It_says_an_untouched_path_holds_nothing()
    {
        // Arrange
        var session = GivenA.CommentSession().WithStore(new RecordingCommentStore("comments.md")).Build();

        // Act
        var holds = await session.HoldsSomethingAtAsync("notes.md");

        // Assert
        Assert.False(holds);
    }
}
