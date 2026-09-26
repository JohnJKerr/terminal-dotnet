namespace TerminalDotnet;

/// <summary>
/// Where a panel's selection stands among its rows. Every panel keeps its
/// selection on a row that exists, and a reload finds the reader's row again
/// wherever it moved to rather than dropping them back to the top.
/// </summary>
internal static class RowSelection
{
    public static int At(int index, int rowCount) => Math.Clamp(index, 0, LastRow(rowCount));

    public static int Up(int selected) => Math.Max(0, selected - 1);

    public static int Down(int selected, int rowCount) => Math.Min(LastRow(rowCount), selected + 1);

    /// <summary>The same row, or the last one when the list has shrunk
    /// beneath it.</summary>
    public static int Kept(int selected, int rowCount) => Math.Min(selected, LastRow(rowCount));

    /// <summary>The row the reader was standing on, wherever it now is. A row
    /// that has gone leaves them where they were standing instead.</summary>
    public static int FoundAgain<T>(IReadOnlyList<T> rows, Func<T, bool> standingOn, int selected)
    {
        for (var index = 0; index < rows.Count; index++)
        {
            if (standingOn(rows[index]))
            {
                return index;
            }
        }

        return At(selected, rows.Count);
    }

    private static int LastRow(int rowCount) => Math.Max(0, rowCount - 1);
}
