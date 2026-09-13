using TerminalDotnet.Search;
using Xunit;

namespace TerminalDotnet.Tests.Explorer;

public sealed class WhenRingingRoundTheRows
{
    [Fact]
    public void It_starts_beside_the_row_it_was_given()
    {
        // Act
        var rows = RowRing.From(count: 5, currentIndex: 3, step: 1);

        // Assert
        Assert.Equal(4, rows[0]);
    }

    [Fact]
    public void It_comes_back_round_to_the_first_row_from_the_last()
    {
        // Act
        var rows = RowRing.From(count: 5, currentIndex: 4, step: 1);

        // Assert
        Assert.Equal([0, 1, 2, 3, 4], rows);
    }

    [Fact]
    public void It_walks_backwards_when_it_is_stepped_back()
    {
        // Act
        var rows = RowRing.From(count: 5, currentIndex: 3, step: -1);

        // Assert
        Assert.Equal([2, 1, 0, 4, 3], rows);
    }

    [Fact]
    public void It_ends_on_the_row_it_started_from()
    {
        // Act
        var rows = RowRing.From(count: 5, currentIndex: 3, step: 1);

        // Assert
        Assert.Equal(3, rows[^1]);
    }

    [Fact]
    public void It_offers_the_only_row_there_is()
    {
        // Act
        var rows = RowRing.From(count: 1, currentIndex: 0, step: 1);

        // Assert
        Assert.Equal([0], rows);
    }

    [Fact]
    public void It_offers_nothing_when_there_are_no_rows()
    {
        // Act
        var rows = RowRing.From(count: 0, currentIndex: 0, step: 1);

        // Assert
        Assert.Empty(rows);
    }
}
