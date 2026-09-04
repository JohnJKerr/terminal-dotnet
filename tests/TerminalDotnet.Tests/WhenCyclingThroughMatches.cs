using TerminalDotnet.Search;
using Xunit;

namespace TerminalDotnet.Tests.Explorer;

public sealed class WhenCyclingThroughMatches
{
    [Fact]
    public void It_moves_to_the_match_after_the_selection()
    {
        // Act
        var index = SelectionRing.Next([2, 5, 8], 2);

        // Assert
        Assert.Equal(5, index);
    }

    [Fact]
    public void It_wraps_to_the_first_match_past_the_last_one()
    {
        // Act
        var index = SelectionRing.Next([2, 5, 8], 8);

        // Assert
        Assert.Equal(2, index);
    }

    [Fact]
    public void It_moves_to_the_match_before_the_selection()
    {
        // Act
        var index = SelectionRing.Previous([2, 5, 8], 8);

        // Assert
        Assert.Equal(5, index);
    }

    [Fact]
    public void It_wraps_to_the_last_match_before_the_first_one()
    {
        // Act
        var index = SelectionRing.Previous([2, 5, 8], 2);

        // Assert
        Assert.Equal(8, index);
    }

    [Fact]
    public void It_finds_nothing_without_matches()
    {
        // Act
        var index = SelectionRing.Next([], 0);

        // Assert
        Assert.Equal(SelectionRing.None, index);
    }
}
