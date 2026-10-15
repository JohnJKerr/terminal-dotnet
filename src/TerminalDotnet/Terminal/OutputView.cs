using System.Drawing;
using Terminal.Gui.Drawing;
using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Attribute = Terminal.Gui.Drawing.Attribute;
using Color = Terminal.Gui.Drawing.Color;

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
    private int currentMatch;
    private IReadOnlyList<OutputMatch> matches = [];
    private IReadOnlySet<(int Line, int Column)> matchedCells = new HashSet<(int, int)>();

    /// <summary>A match stands out from the coloured output around it.</summary>
    private static readonly Attribute MatchAttribute = new(Color.Black, Color.BrightYellow);

    /// <summary>The match n and N have moved to stands out from the rest, so
    /// a step is seen even when the rows on screen stay where they are.</summary>
    private static readonly Attribute CurrentMatchAttribute = new(Color.Black, Color.BrightCyan);

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
        FindMatches();
        Wrap();
        Viewport = Viewport with { Y = following ? LastTop : Math.Min(Viewport.Y, LastTop) };
    }

    /// <summary>Which match the reader is on, such as "match 3 of 12", or
    /// nothing when there is no search.</summary>
    public string SearchSummary => searched.Length == 0 ? ""
        : matches.Count == 0 ? "no matches"
        : $"match {currentMatch + 1} of {matches.Count}";

    /// <summary>Raised when the search, its matches or the reader's place
    /// among them changes.</summary>
    public event EventHandler? SearchChanged;

    /// <summary>Brings the first place the output says
    /// <paramref name="query"/> to the top, ignoring case.</summary>
    public void Search(string query)
    {
        searched = query;
        currentMatch = 0;
        FindMatches();
        SetNeedsDraw();
        if (matches.FirstOrDefault() is { } first)
        {
            ScrollToMatch(first);
        }
    }

    /// <summary>Moves <paramref name="step"/> matches on, going round from
    /// the last to the first and back.</summary>
    private void StepMatch(int step)
    {
        if (matches.Count == 0)
        {
            return;
        }

        currentMatch = (currentMatch + step + matches.Count) % matches.Count;
        ScrollToMatch(matches[currentMatch]);
        SetNeedsDraw();
        SearchChanged?.Invoke(this, EventArgs.Empty);
    }

    private void FindMatches()
    {
        matches = searched.Length == 0
            ? []
            : [.. lines.SelectMany((line, index) => MatchesIn(PlainText(line), index))];
        matchedCells = matches
            .SelectMany(match => Enumerable.Range(match.Column, searched.Length).Select(column => (match.Line, column)))
            .ToHashSet();
        currentMatch = Math.Clamp(currentMatch, 0, Math.Max(0, matches.Count - 1));
        SearchChanged?.Invoke(this, EventArgs.Empty);
    }

    private IEnumerable<OutputMatch> MatchesIn(string text, int line) => Enumerable
        .Range(0, Math.Max(0, text.Length - searched.Length + 1))
        .Where(column => string.Compare(text, column, searched, 0, searched.Length, StringComparison.OrdinalIgnoreCase) == 0)
        .Select(column => new OutputMatch(line, column));

    private static string PlainText(List<Cell> line) => string.Concat(line.Select(cell => cell.Grapheme));

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
        if (key.NoShift.KeyCode == KeyCode.N)
        {
            StepMatch(key.IsShift ? -1 : 1);
            return true;
        }

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

    private Attribute AttributeAt(int line, int column, Cell cell) =>
        InCurrentMatch(line, column) ? CurrentMatchAttribute
        : matchedCells.Contains((line, column)) ? MatchAttribute
        : cell.Attribute ?? GetAttributeForRole(VisualRole.Normal);

    private bool InCurrentMatch(int line, int column) =>
        matches.Count > 0 &&
        matches[currentMatch] is var current &&
        current.Line == line &&
        column >= current.Column &&
        column < current.Column + searched.Length;

    private void DrawRow(OutputRow row, int screenRow)
    {
        Move(0, screenRow);
        foreach (var (cell, offset) in row.Cells.Select((cell, offset) => (cell, offset)))
        {
            SetAttribute(AttributeAt(row.Line, row.Start + offset, cell));
            AddStr(cell.Grapheme);
        }
    }
}

internal sealed record OutputMatch(int Line, int Column);
