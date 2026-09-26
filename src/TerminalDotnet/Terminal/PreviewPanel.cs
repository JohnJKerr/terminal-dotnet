using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using TextMateSharp.Grammars;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace TerminalDotnet.Terminal;

/// <summary>
/// The framed panel that shows the selection of the list it follows: a file's
/// source with its line picked out, or a change's diff. Only one of the two is
/// on show at a time, and a detail such as the compiler's message for an
/// issue sits beneath the source it points at.
/// </summary>
internal sealed class PreviewPanel
{
    private const int DetailRows = 4;

    /// <summary>Source is shown as written, so an underscore is not taken as
    /// the mark of a hot key.</summary>
    private static readonly System.Text.Rune NoHotKey = new(0xFFFF);

    private readonly Code code;
    private readonly ColoredTextView diff;
    private readonly Label highlight;
    private readonly Label details;
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
        code.ViewportChanged += (_, _) => ShowHighlight();
        diff = new ColoredTextView(wordWrap: false)
        {
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            CanFocus = false,
            Visible = false
        };
        ViewColours.ColourGround(diff, () => Background);
        highlight = Highlight();
        details = Details();
        View = new View
        {
            BorderStyle = LineStyle.Single,
            CanFocus = true
        };
        View.Add(code, diff, highlight, details);
        frame = new PanelFrame(View);
        frame.Show(PanelTitle.Segments(PanelKind.Preview, [], "", focused: false), "");
    }

    public View View { get; }

    /// <summary>The title drawn over the preview's frame.</summary>
    public IEnumerable<View> Overlays => frame.Overlays;

    /// <summary>The rows a page of the preview scrolls by.</summary>
    public int PageHeight => ShowingDiff ? diff.Viewport.Height : code.Viewport.Height;

    private bool ShowingDiff => diff.Visible;

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
