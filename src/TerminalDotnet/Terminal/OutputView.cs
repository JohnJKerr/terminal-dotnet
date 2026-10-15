using System.Drawing;
using Terminal.Gui.Drawing;
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

    private static IEnumerable<List<Cell>> WrappedRows(List<Cell> line, int width) =>
        line.Count == 0 || width <= 0 ? [line] : line.Chunk(width).Select(row => row.ToList());

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
