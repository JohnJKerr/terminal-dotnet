using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace TerminalDotnet.Terminal;

/// <summary>
/// The counts the active panel reports along the status line, each in the
/// tone of what it counts. The labels are made once and reused, so a panel
/// reporting fewer counts hides the ones it leaves over.
/// </summary>
internal sealed class StatusSegmentLine
{
    private const int MaxSegments = 4;
    private const int Gap = 2;

    private readonly int firstColumn;
    private IReadOnlyList<StatusSegment> shown = [];

    public StatusSegmentLine(int firstColumn, int rowFromBottom)
    {
        this.firstColumn = firstColumn;
        Labels = [.. Enumerable.Range(0, MaxSegments).Select(index => SegmentLabel(index, rowFromBottom))];
    }

    public IReadOnlyList<Label> Labels { get; }

    public void Show(IReadOnlyList<StatusSegment> segments)
    {
        shown = segments;
        var placed = StatusSegmentLayout.Place(segments, firstColumn, Gap);
        foreach (var (index, label) in Labels.Index())
        {
            Place(label, index < placed.Count ? placed[index] : null);
        }
    }

    public void Hide()
    {
        foreach (var label in Labels)
        {
            label.Visible = false;
        }
    }

    private Label SegmentLabel(int index, int rowFromBottom)
    {
        var label = new Label { X = firstColumn, Y = Pos.AnchorEnd(rowFromBottom), Height = 1, Visible = false };
        ViewColours.ColourText(label, () => RowAppearance.ForegroundFor(ToneOf(index), Color.White));
        return label;
    }

    private RowTone ToneOf(int index) => index < shown.Count ? shown[index].Tone : RowTone.Neutral;

    private static void Place(Label label, PlacedStatusSegment? segment)
    {
        label.Visible = segment is not null;
        if (segment is null)
        {
            return;
        }

        label.X = segment.Column;
        label.Width = segment.Text.Length;
        label.Text = segment.Text;
    }
}
