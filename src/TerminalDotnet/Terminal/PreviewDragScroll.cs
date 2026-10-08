namespace TerminalDotnet.Terminal;

/// <summary>
/// How a drag held past the preview's text scrolls it. The terminal reports
/// the mouse only when it moves, so a drag held still out there is asked
/// after on a clock, and scrolls by its distance from the text each time.
/// </summary>
public static class PreviewDragScroll
{
    public const int MostRows = 5;

    public static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(50);

    /// <param name="row">The row the drag is held on, counted from the top
    /// row of the text on show, which is negative above it.</param>
    /// <param name="height">The rows of text on show.</param>
    public static int RowsFor(int row, int height)
    {
        var past = row < 0 ? row : Math.Max(row - height + 1, 0);
        return Math.Clamp(past, -MostRows, MostRows);
    }
}
