using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Presentation;

public sealed class WhenTitlingTheSearchBox
{
    [Fact]
    public void An_empty_search_is_titled_plainly()
    {
        // Act
        var title = SearchBox.Title("", hitCount: 0);

        // Assert
        Assert.Equal("Search", title);
    }

    [Fact]
    public void A_search_with_one_hit_counts_it_as_one()
    {
        // Act
        var title = SearchBox.Title("order", hitCount: 1);

        // Assert
        Assert.Equal("Search — 1 hit", title);
    }

    [Fact]
    public void A_search_with_several_hits_counts_them_all()
    {
        // Act
        var title = SearchBox.Title("order", hitCount: 4);

        // Assert
        Assert.Equal("Search — 4 hits", title);
    }
}
