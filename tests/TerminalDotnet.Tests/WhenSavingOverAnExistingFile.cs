using TerminalDotnet.Comments;
using Xunit;

namespace TerminalDotnet.Tests.Comments;

public sealed class WhenSavingOverAnExistingFile
{
    [Fact]
    public async Task It_says_a_path_that_already_holds_something_does()
    {
        // Arrange
        var session = new CommentSession(store: new InMemoryCommentStore("comments.md"));

        // Act
        var holds = await session.HoldsSomethingAtAsync("comments.md");

        // Assert
        Assert.True(holds);
    }

    [Fact]
    public async Task It_says_an_untouched_path_holds_nothing()
    {
        // Arrange
        var session = new CommentSession(store: new InMemoryCommentStore("comments.md"));

        // Act
        var holds = await session.HoldsSomethingAtAsync("notes.md");

        // Assert
        Assert.False(holds);
    }

    [Fact]
    public async Task It_says_nothing_is_there_when_there_is_no_store_to_ask()
    {
        // Arrange
        var session = new CommentSession();

        // Act
        var holds = await session.HoldsSomethingAtAsync("comments.md");

        // Assert
        Assert.False(holds);
    }

    private sealed class InMemoryCommentStore(params string[] paths) : ICommentStore
    {
        private readonly HashSet<string> written = [.. paths];

        public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default) =>
            Task.FromResult(written.Contains(path));

        public Task<bool> TryWriteAsync(
            string path,
            string text,
            CancellationToken cancellationToken = default)
        {
            written.Add(path);
            return Task.FromResult(true);
        }
    }
}
