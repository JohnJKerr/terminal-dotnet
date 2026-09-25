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
    private FileRowTone detailTone;
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
        diff.GettingAttributeForRole += (_, args) =>
        {
            args.Result = new Attribute(args.Result?.Foreground ?? Color.White, Background);
            args.Handled = true;
        };
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
        ShowSource(title, "", "plaintext", 1, highlightLine: false, "", FileRowTone.Neutral);
    }

    public void ShowSource(
        string title,
        string text,
        string language,
        int line,
        bool highlightLine,
        string detail,
        FileRowTone tone)
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
        code.ScrollVertical(-code.GetContentSize().Height);
        code.ScrollVertical(Math.Max(0, line - 1 - code.Viewport.Height / 3));
        ShowHighlight();
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
        label.GettingAttributeForRole += (_, args) =>
        {
            args.Result = new Attribute(args.Result?.Foreground ?? Color.White, Color.BrightBlue);
            args.Handled = true;
        };
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
        label.GettingAttributeForRole += (_, args) =>
        {
            args.Result = new Attribute(
                FileRowAppearance.ForegroundFor(detailTone, Color.White),
                args.Result?.Background ?? Color.Black);
            args.Handled = true;
        };
        return label;
    }
}
