using TerminalDotnet.Trust;
using Xunit;

namespace TerminalDotnet.Tests.Trust;

public sealed class WhenAnsweringTheTrustQuestionAsText
{
    [Fact]
    public void It_trusts_the_folder_when_the_reader_says_yes()
    {
        // Act
        var trusted = TrustQuestion.Trusts("y");

        // Assert
        Assert.True(trusted);
    }

    [Fact]
    public void It_trusts_the_folder_when_the_reader_writes_the_word_out()
    {
        // Act
        var trusted = TrustQuestion.Trusts("yes");

        // Assert
        Assert.True(trusted);
    }

    [Fact]
    public void It_reads_an_answer_whatever_its_case_and_spacing()
    {
        // Act
        var trusted = TrustQuestion.Trusts("  Y  ");

        // Assert
        Assert.True(trusted);
    }

    [Fact]
    public void It_quits_when_the_reader_only_presses_enter()
    {
        // Act
        var trusted = TrustQuestion.Trusts("");

        // Assert
        Assert.False(trusted);
    }

    [Fact]
    public void It_quits_when_there_is_no_answer_to_read()
    {
        // Act
        var trusted = TrustQuestion.Trusts(null);

        // Assert
        Assert.False(trusted);
    }

    [Fact]
    public void It_quits_on_an_answer_it_does_not_recognise()
    {
        // Act
        var trusted = TrustQuestion.Trusts("later");

        // Assert
        Assert.False(trusted);
    }

    [Fact]
    public void It_names_the_folder_in_the_question_it_prints()
    {
        // Arrange
        var question = TrustQuestion.For("/work/shop");

        // Act
        var asked = question.AsText();

        // Assert
        Assert.Contains("/work/shop", asked);
    }

    [Fact]
    public void It_offers_the_safe_choice_as_the_one_enter_picks()
    {
        // Arrange
        var question = TrustQuestion.For("/work/shop");

        // Act
        var asked = question.AsText();

        // Assert
        Assert.Contains(question.QuitChoice, asked);
    }
}
