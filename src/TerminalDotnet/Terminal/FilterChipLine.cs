using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace TerminalDotnet.Terminal;

/// <summary>
/// The active panel's filters, beside the search box, with the one in use
/// picked out, so what the panel is hiding is always in view.
/// </summary>
internal sealed class FilterChipLine
{
    private const int MaxChips = 5;
    private const int Gap = 2;

    private readonly View besides;
    private IReadOnlyList<FilterChip> shown = [];

    public FilterChipLine(View besides, int rowFromBottom)
    {
        this.besides = besides;
        Labels = [.. Enumerable.Range(0, MaxChips).Select(index => ChipLabel(index, rowFromBottom))];
    }

    public IReadOnlyList<Label> Labels { get; }

    public void Show(IReadOnlyList<FilterChip> chips)
    {
        shown = chips;
        var columns = StatusSegmentLayout.ColumnsFor([.. chips.Select(chip => chip.Text)], 0, Gap);
        foreach (var (index, label) in Labels.Index())
        {
            label.Visible = index < chips.Count;
            if (!label.Visible)
            {
                continue;
            }

            label.X = Pos.Right(besides) + Gap + columns[index];
            label.Width = chips[index].Text.Length;
            label.Text = chips[index].Text;
        }
    }

    private Label ChipLabel(int index, int rowFromBottom)
    {
        var label = new Label { Y = Pos.AnchorEnd(rowFromBottom), Height = 1, Visible = false };
        ViewColours.ColourText(label, () => FilterAppearance.ForegroundFor(index < shown.Count && shown[index].IsActive));
        return label;
    }
}
