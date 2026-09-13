using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using TerminalDotnet.Comments;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Comments;

public sealed class WhenPressingAKeyInTheCommentPanel
{
    [Fact]
    public void Pressing_enter_reads_the_comment()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.Enter));

        // Assert
        Assert.Equal(new CommentAction.ReadComment(), action);
    }

    [Fact]
    public void Pressing_v_reads_the_comment()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.V));

        // Assert
        Assert.Equal(new CommentAction.ReadComment(), action);
    }

    [Fact]
    public void Pressing_e_rewrites_the_comment()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.E));

        // Assert
        Assert.Equal(new CommentAction.RewriteComment(), action);
    }

    [Fact]
    public void Pressing_d_deletes_the_comment()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.D));

        // Assert
        Assert.Equal(new CommentAction.DeleteComment(), action);
    }

    [Fact]
    public void Pressing_a_key_with_nothing_commented_does_nothing()
    {
        // Act
        var action = CommentPanelKeyBindings.ActionFor(new Key(KeyCode.E), null, searchActive: false);

        // Assert
        Assert.Null(action);
    }

    [Fact]
    public void Pressing_a_key_while_searching_types_it_instead()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.E), searchActive: true);

        // Assert
        Assert.Null(action);
    }

    [Fact]
    public void Pressing_y_copies_the_comments_to_the_clipboard()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.Y));

        // Assert
        Assert.Equal(new CommentAction.CopyComments(), action);
    }

    private static CommentAction? ActionFor(Key key, bool searchActive = false) =>
        CommentPanelKeyBindings.ActionFor(
            key,
            new FileComment("/repo/src/Order.cs", "src/Order.cs", "needs a guard"),
            searchActive);
}
