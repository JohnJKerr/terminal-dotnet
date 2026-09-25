using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Terminal;

public sealed class WhenAskingAboutTheComments
{
    [Fact]
    public void Quitting_on_a_single_note_counts_it_as_one()
    {
        // Act
        var warning = CommentPrompt.QuitLoses(1);

        // Assert
        Assert.Equal("1 comment has not been copied or saved. Quitting loses it.", warning);
    }

    [Fact]
    public void Quitting_on_several_notes_counts_them_all()
    {
        // Act
        var warning = CommentPrompt.QuitLoses(3);

        // Assert
        Assert.Equal("3 comments have not been copied or saved. Quitting loses them.", warning);
    }

    [Fact]
    public void Clearing_a_single_note_counts_it_as_one()
    {
        // Act
        var question = CommentPrompt.ClearAll(1);

        // Assert
        Assert.Equal("Clear 1 comment? This cannot be undone.", question);
    }

    [Fact]
    public void Clearing_several_notes_counts_them_all()
    {
        // Act
        var question = CommentPrompt.ClearAll(3);

        // Assert
        Assert.Equal("Clear all 3 comments? This cannot be undone.", question);
    }
}
