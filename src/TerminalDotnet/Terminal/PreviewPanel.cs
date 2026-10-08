using System.Drawing;
using Terminal.Gui.Drawing;
using Terminal.Gui.Text;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using TextMateSharp.Grammars;
using Attribute = Terminal.Gui.Drawing.Attribute;
using Color = Terminal.Gui.Drawing.Color;

namespace TerminalDotnet.Terminal;

/// <summary>
/// The framed panel that shows the selection of the list it follows: a file's
/// source with its line picked out, or a change's diff. Only one of the two is
/// on show at a time, and a detail such as the compiler's message for an
/// issue sits beneath the source it points at.
///
/// A drag across either selects the text under it. Neither view selects for
/// itself, so the panel keeps the selection and marks it over whichever is
/// on show.
/// </summary>
internal sealed class PreviewPanel
{
    private const int DetailRows = 4;

    /// <summary>The diff's text view draws its tabs out to stops this far
    /// apart, while the source view draws each as a single cell.</summary>
    private const int DiffTabStop = 4;

    /// <summary>Source is shown as written, so an underscore is not taken as
    /// the mark of a hot key.</summary>
    private static readonly System.Text.Rune NoHotKey = new(0xFFFF);

    private readonly Code code;
    private readonly ColoredTextView diff;
    private readonly Label highlight;
    private readonly Label details;
    private readonly View marks;
    private IReadOnlyList<string> shownLines = [];
    private PreviewSelection? selection;
    private int highlightedLine = 1;
    private bool highlighting;
    private RowTone detailTone;
    private readonly PanelFrame frame;

    public PreviewPanel()
    {
        code = new Code
        {
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            CanFocus = false,
            SyntaxHighlighter = new TextMateSyntaxHighlighter(ThemeName.DarkPlus)
        };
        code.GettingAttributeForRole += (_, args) =>
        {
            args.Result = new Attribute(PreviewCodeAppearance.ForegroundFor(args.Role), Background);
            args.Handled = true;
        };
        code.ViewportChanged += (_, _) =>
        {
            ShowHighlight();
            MarkAgain();
        };
        diff = new ColoredTextView(wordWrap: false)
        {
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            CanFocus = false,
            Visible = false,
            TabWidth = DiffTabStop
        };
        diff.ViewportChanged += (_, _) => MarkAgain();
        ViewColours.ColourGround(diff, () => Background);
        highlight = Highlight();
        details = Details();
        marks = Marks();
        View = new View
        {
            BorderStyle = LineStyle.Single,
            CanFocus = true
        };
        View.Add(code, diff, highlight, details, marks);
        frame = new PanelFrame(View);
        frame.Show(PanelTitle.Segments(PanelKind.Preview, [], "", focused: false), "");
    }

    public View View { get; }

    /// <summary>The title drawn over the preview's frame.</summary>
    public IEnumerable<View> Overlays => frame.Overlays;

    /// <summary>The rows a page of the preview scrolls by.</summary>
    public int PageHeight => ShowingDiff ? diff.Viewport.Height : code.Viewport.Height;

    private bool ShowingDiff => diff.Visible;

    private View Shown => ShowingDiff ? diff : code;

    /// <summary>The preview is drawn on the same ground as the lists beside
    /// it, rather than the lighter one a text view brings with it.</summary>
    private Color Background => View.GetAttributeForRole(VisualRole.Normal).Background;

    public void ShowNothing(string title)
    {
        ShowSource(title, "", "plaintext", 1, highlightLine: false, "", RowTone.Neutral);
    }

    public void ShowSource(
        string title,
        string text,
        string language,
        int line,
        bool highlightLine,
        string detail,
        RowTone tone)
    {
        frame.Show([new TitleSegment(title, false)], "");
        diff.Visible = false;
        code.Visible = true;
        code.Language = language;
        code.Text = text;
        Select(null, text.ReplaceLineEndings("\n").Split('\n'));
        highlightedLine = line;
        highlighting = highlightLine;
        detailTone = tone;
        details.Text = detail;
        details.Visible = detail.Length > 0;
        code.Height = Dim.Fill(details.Visible ? DetailRows : 0);
        ScrollToLine(line);
        ShowHighlight();
    }

    /// <summary>A new file starts from its top, wherever the last one was
    /// left: scrolling up by the new file's length is not enough when the
    /// last one was longer and read to its end. The line to highlight is then
    /// brought a third of the way down.</summary>
    private void ScrollToLine(int line)
    {
        code.Viewport = code.Viewport with { X = 0, Y = 0 };
        code.ScrollVertical(Math.Max(0, line - 1 - code.Viewport.Height / 3));
    }

    public void ShowDiff(string title, IReadOnlyList<DiffLine> lines)
    {
        frame.Show([new TitleSegment(title, false)], "");
        code.Visible = false;
        highlight.Visible = false;
        details.Visible = false;
        diff.Visible = true;
        Select(null, [.. lines.Select(line => line.Text)]);
        diff.Load(lines
            .Select(line => Cell.ToCellList(line.Text, new Attribute(DiffAppearance.ForegroundFor(line.Tone), Background)))
            .ToList());
    }

    public void Scroll(int rows)
    {
        if (ShowingDiff)
        {
            diff.ScrollVertical(rows);
            return;
        }

        code.ScrollVertical(rows);
    }

    public void ScrollToStart() => Scroll(-ContentHeight);

    public void ScrollToEnd() => Scroll(ContentHeight);

    private int ContentHeight => ShowingDiff ? diff.GetContentSize().Height : code.GetContentSize().Height;

    public void Place(PanelArea area)
    {
        View.X = area.X;
        View.Y = area.Y;
        View.Width = area.Width;
        View.Height = area.Height;
        View.Visible = area != PanelLayout.Hidden;
        frame.Place(area);
    }

    /// <summary>Starts a selection at a cell of the text on show. A press
    /// anywhere else, such as on a scroll bar or in another panel, lets go of
    /// the one there was.</summary>
    public void SelectFrom(View? pressed, Point screen)
    {
        var cell = CellAt(screen);
        var onTheText = pressed == Shown || pressed == highlight;
        Select(onTheText ? new PreviewSelection(cell, cell, ShowingDiff ? DiffTabStop : 1) : null, shownLines);
    }

    /// <summary>Carries the selection on to wherever the drag has reached. A
    /// drag that leaves the text keeps to the nearest cell of it.</summary>
    public void SelectTo(Point screen)
    {
        if (selection is null)
        {
            return;
        }

        Select(selection with { To = CellAt(screen) }, shownLines);
    }

    /// <summary>The text the last drag selected, or none.</summary>
    public string SelectedText => selection?.TextIn(shownLines) ?? "";

    private PreviewCell CellAt(Point screen)
    {
        var viewport = Shown.Viewport;
        var at = Shown.ScreenToViewport(screen);
        return new PreviewCell(
            viewport.Y + Math.Clamp(at.Y, 0, Math.Max(viewport.Height - 1, 0)),
            viewport.X + Math.Clamp(at.X, 0, Math.Max(viewport.Width - 1, 0)));
    }

    private void Select(PreviewSelection? selected, IReadOnlyList<string> lines)
    {
        shownLines = lines;
        if (selection == selected)
        {
            return;
        }

        selection = selected;
        MarkAgain();
    }

    /// <summary>The marks are drawn over the views beneath them, so a mark
    /// that moves or goes needs those drawn again as well.</summary>
    private void MarkAgain() => View.SetNeedsDraw();

    private void DrawMarks(DrawContext? context)
    {
        if (selection is null)
        {
            return;
        }

        var viewport = Shown.Viewport;
        marks.SetAttribute(new Attribute(Color.Black, Color.White));
        foreach (var stretch in selection.MarkedIn(shownLines, viewport.Y, viewport.Height))
        {
            DrawMark(stretch, viewport, context);
        }
    }

    /// <summary>Each cell drawn is reported, because the marks are otherwise
    /// see-through and only what they report is kept from the views beneath.
    /// </summary>
    private void DrawMark(SelectedStretch stretch, Rectangle viewport, DrawContext? context)
    {
        var row = stretch.Line - viewport.Y;
        var column = stretch.Column - viewport.X;
        foreach (var grapheme in GraphemeHelper.GetGraphemes(stretch.Text))
        {
            var width = Math.Max(grapheme.GetColumns(), 1);
            if (column >= 0 && column + width <= viewport.Width)
            {
                marks.AddStr(column, row, grapheme);
                context?.AddDrawnRectangle(marks.ViewportToScreen(new Rectangle(column, row, width, 1)));
            }

            column += width;
        }
    }

    private View Marks()
    {
        var view = new View
        {
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            CanFocus = false,
            ViewportSettings = ViewportSettingsFlags.Transparent | ViewportSettingsFlags.TransparentMouse
        };
        view.DrawingContent += (_, args) => DrawMarks(args.DrawContext);
        return view;
    }

    private void ShowHighlight()
    {
        var shown = PreviewSourceHighlight.From(
            code.Text,
            highlightedLine,
            code.Viewport.Y,
            code.Viewport.Height,
            highlighting && code.Visible);
        highlight.Text = shown.Text;
        highlight.Y = shown.Row;
        highlight.Visible = shown.Visible;
    }

    private static Label Highlight()
    {
        var label = new Label { Width = Dim.Fill(), Height = 1, Visible = false, HotKeySpecifier = NoHotKey };
        ViewColours.ColourGround(label, () => Color.BrightBlue);
        return label;
    }

    private Label Details()
    {
        var label = new Label
        {
            Y = Pos.AnchorEnd(DetailRows),
            Width = Dim.Fill(),
            Height = DetailRows,
            Visible = false,
            HotKeySpecifier = NoHotKey
        };
        label.TextFormatter.MultiLine = true;
        label.TextFormatter.WordWrap = true;
        ViewColours.ColourText(label, () => RowAppearance.ForegroundFor(detailTone, Color.White));
        return label;
    }
}
