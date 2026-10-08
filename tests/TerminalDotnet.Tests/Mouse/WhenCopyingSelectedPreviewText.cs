using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Mouse;

/// <summary>Letting go of a drag copies what it selected, and a toast says
/// how much went to the clipboard, or that none of it did.</summary>
public sealed class WhenCopyingSelectedPreviewText
{
    [Fact]
    public void It_says_how_many_characters_were_copied()
    {
        // Act
        var toast = SelectionToast.Copied("sealed");

        // Assert
        Assert.Equal("Copied 6 characters", toast.Text);
    }

    [Fact]
    public void Text_from_several_lines_is_counted_in_characters_too()
    {
        // Act
        var toast = SelectionToast.Copied("class Cart\n{\n    private");

        // Assert
        Assert.Equal("Copied 24 characters", toast.Text);
    }

    [Fact]
    public void A_wide_character_counts_once()
    {
        // Act
        var toast = SelectionToast.Copied("合");

        // Assert
        Assert.Equal("Copied 1 character", toast.Text);
    }

    [Fact]
    public void A_copy_reads_as_a_success()
    {
        // Act
        var toast = SelectionToast.Copied("sealed");

        // Assert
        Assert.Equal(ToastTone.Succeeded, toast.Tone);
    }

    [Fact]
    public void A_copy_that_reached_no_clipboard_says_so()
    {
        // Act
        var toast = SelectionToast.NotCopied();

        // Assert
        Assert.Equal("Could not copy the selection", toast.Text);
    }

    [Fact]
    public void A_copy_that_reached_no_clipboard_reads_as_a_failure()
    {
        // Act
        var toast = SelectionToast.NotCopied();

        // Assert
        Assert.Equal(ToastTone.Failed, toast.Tone);
    }

    [Fact]
    public void It_fades_away_on_its_own()
    {
        // Act
        var toast = SelectionToast.Copied("sealed");

        // Assert
        Assert.True(toast.FadesAway);
    }
}
