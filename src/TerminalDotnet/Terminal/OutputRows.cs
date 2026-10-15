using Terminal.Gui.Drawing;

namespace TerminalDotnet.Terminal;

/// <summary>A row on screen: the cells of <paramref name="Line"/> from
/// <paramref name="Start"/> that fit across the view.</summary>
internal sealed record OutputRow(int Line, int Start, List<Cell> Cells)
{
    public bool Holds(int line, int column) => Line == line && column >= Start && column < Start + Cells.Count;
}

internal static class OutputRows
{
    public static IReadOnlyList<OutputRow> Wrap(IReadOnlyList<List<Cell>> lines, int width) =>
        [.. lines.SelectMany((line, index) => WrappedRows(line, index, width))];

    private static IEnumerable<OutputRow> WrappedRows(List<Cell> line, int index, int width)
    {
        if (width <= 0)
        {
            yield return new OutputRow(index, 0, line);
            yield break;
        }

        var start = 0;
        while (line.Count - start > width)
        {
            var length = RowLength(line, start, width);
            yield return new OutputRow(index, start, line.GetRange(start, length));
            start += length;
        }

        yield return new OutputRow(index, start, line.GetRange(start, line.Count - start));
    }

    /// <summary>A row ends after the last space that fits, so a word is
    /// carried whole onto the next row; a word wider than a row is broken.
    /// </summary>
    private static int RowLength(List<Cell> line, int start, int width)
    {
        var lastSpace = line.FindLastIndex(start + width - 1, width - 1, cell => cell.Grapheme == " ");
        return lastSpace > start ? lastSpace - start + 1 : width;
    }
}
