using System.Drawing;
using Terminal.Gui.Drawing;
using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace TerminalDotnet.Terminal;

/// <summary>
/// Coloured output to read and scroll. Output is replaced while a run is
/// reported, so the view keeps nothing that depends on what it showed last:
/// it wraps the lines to its width and draws only the rows on screen.
/// </summary>
public sealed class OutputView : View
{
    private IReadOnlyList<List<Cell>> lines = [];
    private IReadOnlyList<List<Cell>> rows = [];
    private int wrappedWidth;

    /// <summary>Whether the output is still being reported, so a reader at
    /// its end stays there to see the latest of it.</summary>
    public bool FollowsTheRun { get; init; }

    public void Show(IReadOnlyList<List<Cell>> output)
    {
        var following = FollowsTheRun && AtTheEnd;
        lines = output;
        Wrap();
        Viewport = Viewport with { Y = following ? LastTop : Math.Min(Viewport.Y, LastTop) };
    }

    private bool AtTheEnd => Viewport.Y >= LastTop;

    /// <summary>The first row on screen when the last row is at the bottom.</summary>
    private int LastTop => Math.Max(0, rows.Count - Viewport.Height);

    protected override bool OnKeyDown(Key key)
    {
        if (RowsToScroll(key) is not { } scrolled)
        {
            return false;
        }

        ScrollBy(scrolled);
        return true;
    }

    protected override bool OnMouseEvent(Mouse mouse)
    {
        if (!mouse.IsWheel)
        {
            return false;
        }

        ScrollBy(mouse.Flags.HasFlag(MouseFlags.WheeledDown) ? WheelRows : -WheelRows);
        return true;
    }

    private void ScrollBy(int scrolled) =>
        Viewport = Viewport with { Y = Math.Clamp(Viewport.Y + scrolled, 0, LastTop) };

    /// <summary>A page keeps one row of the last screen, so the reader has a
    /// line of context to carry across the jump.</summary>
    private int? RowsToScroll(Key key) => key.NoShift.KeyCode switch
    {
        KeyCode.CursorDown or KeyCode.J => 1,
        KeyCode.CursorUp or KeyCode.K => -1,
        KeyCode.PageDown => PageRows,
        KeyCode.PageUp => -PageRows,
        KeyCode.Home => -rows.Count,
        KeyCode.End => rows.Count,
        _ => null
    };

    /// <summary>A notch of the wheel moves a few rows, so long output can be
    /// crossed without a key.</summary>
    private const int WheelRows = 3;

    private int PageRows => Math.Max(1, Viewport.Height - 1);

    protected override void OnViewportChanged(DrawEventArgs e)
    {
        if (Viewport.Width != wrappedWidth)
        {
            Wrap();
        }
    }

    protected override bool OnDrawingContent(DrawContext? context)
    {
        ClearViewport(context);
        foreach (var (row, screenRow) in VisibleRows())
        {
            DrawRow(row, screenRow);
        }

        return true;
    }

    private void Wrap()
    {
        wrappedWidth = Viewport.Width;
        rows = [.. lines.SelectMany(line => WrappedRows(line, wrappedWidth))];
        SetContentSize(new Size(wrappedWidth, rows.Count));
        SetNeedsDraw();
    }

    private static IEnumerable<List<Cell>> WrappedRows(List<Cell> line, int width)
    {
        if (width <= 0)
        {
            yield return line;
            yield break;
        }

        var start = 0;
        while (line.Count - start > width)
        {
            var length = RowLength(line, start, width);
            yield return line.GetRange(start, length);
            start += length;
        }

        yield return line.GetRange(start, line.Count - start);
    }

    /// <summary>A row ends after the last space that fits, so a word is
    /// carried whole onto the next row; a word wider than a row is broken.
    /// </summary>
    private static int RowLength(List<Cell> line, int start, int width)
    {
        var lastSpace = line.FindLastIndex(start + width - 1, width - 1, cell => cell.Grapheme == " ");
        return lastSpace > start ? lastSpace - start + 1 : width;
    }

    private IEnumerable<(List<Cell> Row, int ScreenRow)> VisibleRows() => Enumerable
        .Range(0, Viewport.Height)
        .Where(screenRow => Viewport.Y + screenRow < rows.Count)
        .Select(screenRow => (rows[Viewport.Y + screenRow], screenRow));

    private void DrawRow(List<Cell> row, int screenRow)
    {
        Move(0, screenRow);
        foreach (var cell in row)
        {
            SetAttribute(cell.Attribute ?? GetAttributeForRole(VisualRole.Normal));
            AddStr(cell.Grapheme);
        }
    }
}
