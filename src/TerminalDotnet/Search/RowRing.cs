namespace TerminalDotnet.Search;

/// <summary>
/// The rows of a list in the order they should be tried, starting beside the
/// one in hand and coming back round to it. Stepping through a panel skips
/// rows that have nothing to show, so the caller needs the whole ring rather
/// than only the row next door.
/// </summary>
public static class RowRing
{
    public static IReadOnlyList<int> From(int count, int currentIndex, int step) => count == 0
        ? []
        : [.. Enumerable
            .Range(1, count)
            .Select(offset => Wrapped(currentIndex + (offset * step), count))];

    private static int Wrapped(int index, int count) => ((index % count) + count) % count;
}
