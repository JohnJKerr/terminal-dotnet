namespace TerminalDotnet.Terminal;

public sealed record StatusSegment(string Text, RowTone Tone);

public sealed record PlacedStatusSegment(string Text, RowTone Tone, int Column);

public static class StatusSegmentLayout
{
    public static IReadOnlyList<PlacedStatusSegment> Place(
        IReadOnlyList<StatusSegment> segments,
        int firstColumn,
        int gap)
    {
        var columns = ColumnsFor(segments.Select(segment => segment.Text).ToArray(), firstColumn, gap);
        return segments
            .Select((segment, index) => new PlacedStatusSegment(segment.Text, segment.Tone, columns[index]))
            .ToArray();
    }

    public static IReadOnlyList<int> ColumnsFor(
        IReadOnlyList<string> texts,
        int firstColumn,
        int gap)
    {
        var column = firstColumn;
        var columns = new List<int>();
        foreach (var text in texts)
        {
            columns.Add(column);
            column += text.Length + gap;
        }

        return columns;
    }
}
