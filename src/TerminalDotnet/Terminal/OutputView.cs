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
    private IReadOnlyList<OutputRow> rows = [];
    private int wrappedWidth;
    private string searched = "";

    public OutputView()
    {
        ViewportSettings |= ViewportSettingsFlags.HasVerticalScrollBar;
    }

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

    /// <summary>Brings the first place the output says
    /// <paramref name="query"/> to the top, ignoring case.</summary>
    public void Search(string query)
    {
        searched = query;
        if (Matches().FirstOrDefault() is { } first)
        {
            ScrollToMatch(first);
        }
    }

    private IEnumerable<OutputMatch> Matches() => searched.Length == 0
        ? []
        : lines.SelectMany((line, index) => MatchesIn(Text(line), index));

    private IEnumerable<OutputMatch> MatchesIn(string text, int line) => Enumerable
        .Range(0, Math.Max(0, text.Length - searched.Length + 1))
        .Where(column => string.Compare(text, column, searched, 0, searched.Length, StringComparison.OrdinalIgnoreCase) == 0)
        .Select(column => new OutputMatch(line, column));

    private static string Text(List<Cell> line) => string.Concat(line.Select(cell => cell.Grapheme));

    private void ScrollToMatch(OutputMatch match)
    {
        var row = Math.Max(0, rows.ToList().FindIndex(candidate => candidate.Holds(match.Line, match.Column)));
        Viewport = Viewport with { Y = Math.Min(row, LastTop) };
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
        rows = OutputRows.Wrap(lines, wrappedWidth);
        SetContentSize(new Size(wrappedWidth, rows.Count));
        SetNeedsDraw();
    }

    private IEnumerable<(OutputRow Row, int ScreenRow)> VisibleRows() => Enumerable
        .Range(0, Viewport.Height)
        .Where(screenRow => Viewport.Y + screenRow < rows.Count)
        .Select(screenRow => (rows[Viewport.Y + screenRow], screenRow));

    private void DrawRow(OutputRow row, int screenRow)
    {
        Move(0, screenRow);
        foreach (var cell in row.Cells)
        {
            SetAttribute(cell.Attribute ?? GetAttributeForRole(VisualRole.Normal));
            AddStr(cell.Grapheme);
        }
    }
}

internal sealed record OutputMatch(int Line, int Column);
