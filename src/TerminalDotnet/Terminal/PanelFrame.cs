using System.Text;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace TerminalDotnet.Terminal;

/// <summary>
/// The words drawn over a panel's frame, and the frame's colour. The frame
/// turns green while its panel takes the keys. A title is laid over the top
/// edge a word at a time, so a filter in use can be drawn in the filter
/// colour; the footer sits over the bottom edge. Both are drawn beside the
/// panel rather than in it, because a view draws nothing on its own border.
/// </summary>
internal sealed class PanelFrame
{
    private const int MaxTitleSegments = 8;
    private const int CornerWidth = 1;
    private static readonly Rune NoHotKey = new(0xFFFF);

    private readonly View framed;
    private readonly IReadOnlyList<Label> titleLabels;
    private IReadOnlyList<TitleSegment> title = [];
    private PanelArea area = new(0, 0, 0, 0);

    public PanelFrame(View framed)
    {
        this.framed = framed;
        framed.Title = "";
        framed.HasFocusChanged += (_, _) => framed.SetNeedsDraw();
        if (framed.Border?.View is View border)
        {
            border.GettingAttributeForRole += (_, args) =>
            {
                if (!framed.HasFocus)
                {
                    return;
                }

                args.Result = new Attribute(Color.BrightGreen, args.Result?.Background ?? Color.Black);
                args.Handled = true;
            };
        }

        titleLabels = [.. Enumerable.Range(0, MaxTitleSegments).Select(TitleLabel)];
        Footer = new Label { Height = 1, HotKeySpecifier = NoHotKey };
    }

    public Label Footer { get; }

    /// <summary>The labels to add beside the framed view.</summary>
    public IEnumerable<View> Overlays => [.. titleLabels, Footer];

    public void Show(IReadOnlyList<TitleSegment> segments, string footer)
    {
        title = segments;
        Footer.Text = footer;
        Arrange();
    }

    public void Place(PanelArea placed)
    {
        area = placed;
        Arrange();
    }

    private void Arrange()
    {
        var right = area.X + area.Width - CornerWidth;
        var column = area.X + CornerWidth + 1;
        foreach (var (label, index) in titleLabels.Select((label, index) => (label, index)))
        {
            var text = index < title.Count ? $" {title[index].Text}" : "";
            label.Visible = text.Length > 0 && column + text.Length <= right;
            label.Text = text;
            label.Width = text.Length;
            label.X = column;
            label.Y = area.Y;
            column += text.Length;
        }

        Footer.Visible = area != PanelLayout.Hidden;
        Footer.Width = Footer.Text.Length;
        Footer.X = Math.Max(area.X, right - Footer.Text.Length - 1);
        Footer.Y = area.Y + area.Height - 1;
    }

    private Label TitleLabel(int index)
    {
        var label = new Label { Height = 1, Visible = false, HotKeySpecifier = NoHotKey };
        label.GettingAttributeForRole += (_, args) =>
        {
            if (index >= title.Count || !title[index].IsActive)
            {
                return;
            }

            args.Result = new Attribute(
                FilterAppearance.ForegroundFor(isActive: true),
                args.Result?.Background ?? Color.Black);
            args.Handled = true;
        };
        return label;
    }
}
