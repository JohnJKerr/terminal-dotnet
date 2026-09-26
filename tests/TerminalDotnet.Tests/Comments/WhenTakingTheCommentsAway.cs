using TerminalDotnet.Comments;
using TerminalDotnet.Tests.Builders;
using TerminalDotnet.Tests.Fakes;
using Xunit;

namespace TerminalDotnet.Tests.Comments;

public sealed class WhenTakingTheCommentsAway
{
    [Fact]
    public async Task It_copies_every_note_to_the_clipboard()
    {
        // Arrange
        var clipboard = new RecordingClipboard();
        var session = await GivenA.CommentSession().WithTwoNotes().WithClipboard(clipboard).BuildAsync();

        // Act
        await session.DispatchAsync(new CommentCommand.CopyAll());

        // Assert
        Assert.Equal(
            "## src/Customer.cs\n\nrename this\n\n## src/Order.cs\n\nneeds a guard\n",
            clipboard.Copied);
    }

    [Fact]
    public async Task It_copies_the_notes_a_search_has_hidden_as_well()
    {
        // Arrange
        var clipboard = new RecordingClipboard();
        var session = await GivenA.CommentSession().WithTwoNotes().WithClipboard(clipboard).BuildAsync();
        await session.DispatchAsync(new CommentCommand.Search("order"));

        // Act
        await session.DispatchAsync(new CommentCommand.CopyAll());

        // Assert
        Assert.Contains("src/Customer.cs", clipboard.Copied);
    }

    [Fact]
    public async Task It_says_how_many_notes_were_copied()
    {
        // Arrange
        var session = await GivenA.CommentSession().WithTwoNotes().BuildAsync();

        // Act
        await session.DispatchAsync(new CommentCommand.CopyAll());

        // Assert
        Assert.Equal("Copied 2 comments", session.State.Notice);
    }

    [Fact]
    public async Task It_counts_a_single_copied_note_as_one()
    {
        // Arrange
        var session = await GivenA.CommentSession().WithNote("src/Order.cs", "needs a guard").BuildAsync();

        // Act
        await session.DispatchAsync(new CommentCommand.CopyAll());

        // Assert
        Assert.Equal("Copied 1 comment", session.State.Notice);
    }

    [Fact]
    public async Task It_says_so_when_the_clipboard_would_not_take_them()
    {
        // Arrange
        var session = await GivenA.CommentSession()
            .WithTwoNotes()
            .WithClipboard(new RecordingClipboard { CopySucceeds = false })
            .BuildAsync();

        // Act
        await session.DispatchAsync(new CommentCommand.CopyAll());

        // Assert
        Assert.Equal("Could not copy the comments", session.State.Notice);
    }

    [Fact]
    public async Task It_leaves_the_clipboard_alone_when_nothing_is_commented()
    {
        // Arrange
        var clipboard = new RecordingClipboard();
        var session = GivenA.CommentSession().WithClipboard(clipboard).Build();

        // Act
        await session.DispatchAsync(new CommentCommand.CopyAll());

        // Assert
        Assert.Null(clipboard.Copied);
    }

    [Fact]
    public async Task It_writes_every_note_to_the_file()
    {
        // Arrange
        var store = new RecordingCommentStore();
        var session = await GivenA.CommentSession().WithTwoNotes().WithStore(store).BuildAsync();

        // Act
        await session.DispatchAsync(new CommentCommand.SaveAll("comments.md"));

        // Assert
        Assert.Equal(
            "## src/Customer.cs\n\nrename this\n\n## src/Order.cs\n\nneeds a guard\n",
            store.Written);
    }

    [Fact]
    public async Task It_writes_the_notes_a_search_has_hidden_as_well()
    {
        // Arrange
        var store = new RecordingCommentStore();
        var session = await GivenA.CommentSession().WithTwoNotes().WithStore(store).BuildAsync();
        await session.DispatchAsync(new CommentCommand.Search("order"));

        // Act
        await session.DispatchAsync(new CommentCommand.SaveAll("comments.md"));

        // Assert
        Assert.Contains("src/Customer.cs", store.Written);
    }

    [Fact]
    public async Task It_says_where_the_notes_were_saved()
    {
        // Arrange
        var session = await GivenA.CommentSession().WithTwoNotes().BuildAsync();

        // Act
        await session.DispatchAsync(new CommentCommand.SaveAll("comments.md"));

        // Assert
        Assert.Equal("Saved 2 comments to comments.md", session.State.Notice);
    }

    [Fact]
    public async Task It_counts_a_single_saved_note_as_one()
    {
        // Arrange
        var session = await GivenA.CommentSession().WithNote("src/Order.cs", "needs a guard").BuildAsync();

        // Act
        await session.DispatchAsync(new CommentCommand.SaveAll("comments.md"));

        // Assert
        Assert.Equal("Saved 1 comment to comments.md", session.State.Notice);
    }

    [Fact]
    public async Task It_says_so_when_the_notes_could_not_be_saved()
    {
        // Arrange
        var session = await GivenA.CommentSession()
            .WithTwoNotes()
            .WithStore(new RecordingCommentStore { WriteSucceeds = false })
            .BuildAsync();

        // Act
        await session.DispatchAsync(new CommentCommand.SaveAll("comments.md"));

        // Assert
        Assert.Equal("Could not save the comments to comments.md", session.State.Notice);
    }

    [Fact]
    public async Task It_leaves_the_file_alone_when_nothing_is_commented()
    {
        // Arrange
        var store = new RecordingCommentStore();
        var session = GivenA.CommentSession().WithStore(store).Build();

        // Act
        await session.DispatchAsync(new CommentCommand.SaveAll("comments.md"));

        // Assert
        Assert.Null(store.Written);
    }
}
