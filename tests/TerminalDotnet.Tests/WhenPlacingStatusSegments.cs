using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Terminal;

public sealed class WhenPlacingStatusSegments
{
    [Fact]
    public void It_places_the_first_segment_at_the_opening_column()
    {
        // Arrange
        IReadOnlyList<FileStatusSegment> segments = [new("3 Files", FileRowTone.Neutral)];

        // Act
        var placed = StatusSegmentLayout.Place(segments, firstColumn: 22, gap: 2);

        // Assert
        Assert.Equal(new PlacedStatusSegment("3 Files", FileRowTone.Neutral, 22), placed.Single());
    }

    [Fact]
    public void It_leaves_a_gap_after_each_segment()
    {
        // Arrange
        IReadOnlyList<FileStatusSegment> segments =
        [
            new("3 Files", FileRowTone.Neutral),
            new("1 Added", FileRowTone.New),
            new("2 Edited", FileRowTone.Modified)
        ];

        // Act
        var placed = StatusSegmentLayout.Place(segments, firstColumn: 22, gap: 2);

        // Assert
        Assert.Equal([22, 31, 40], placed.Select(segment => segment.Column));
    }

    [Fact]
    public void It_places_nothing_without_segments()
    {
        // Act
        var placed = StatusSegmentLayout.Place([], firstColumn: 22, gap: 2);

        // Assert
        Assert.Empty(placed);
    }
}
