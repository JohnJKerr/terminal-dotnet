using System.Collections.ObjectModel;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace TerminalDotnet.Terminal;

/// <summary>A row of a list panel, drawn in its own colour or, when it has
/// none, in the panel's.</summary>
internal sealed record ListRow(string Text, Color? Foreground)
{
    public static ListRow Toned(string text, RowTone tone) => new(
        text,
        tone == RowTone.Neutral ? null : RowAppearance.ForegroundFor(tone, Color.White));
}

/// <summary>
/// One of the framed lists that tile the screen. Every panel is drawn at once,
/// so each keeps the rows it last listed and lists them again only when its
/// panel hands it something new.
/// </summary>
internal sealed class ListPanel
{
    private object? listed;
    private string listedMessage = "";
    private IReadOnlyList<Color?> foregrounds = [];
    private readonly PanelFrame frame;
    private bool showing;

    public ListPanel()
    {
        View = new ListView
        {
            BorderStyle = LineStyle.Single,
            ShowMarks = false,
            KeystrokeNavigator = null
        };
        View.RowRender += (_, args) => ColorRow(args);
        View.ValueChanged += (_, _) => ReportChosenRow();
        frame = new PanelFrame(View);
    }

    /// <summary>A row the reader picked in the list itself, such as with a
    /// click, rather than one the panel's session moved to.</summary>
    public event Action<int>? RowChosen;

    public ListView View { get; }

    /// <summary>The title and footer drawn over the list's frame.</summary>
    public IEnumerable<View> Overlays => frame.Overlays;

    /// <summary>A panel with nothing to list says why in place of its rows,
    /// and has no row to select. A message such as the discovery marker moves
    /// while the content stays the same, so either one changing lists again.
    /// </summary>
    public void Show(
        IReadOnlyList<TitleSegment> title,
        string footer,
        object content,
        Func<IReadOnlyList<ListRow>> rows,
        int selectedIndex,
        string emptyMessage)
    {
        frame.Show(title, footer);
        showing = true;
        try
        {
            if (!ReferenceEquals(listed, content) || listedMessage != emptyMessage)
            {
                listed = content;
                listedMessage = emptyMessage;
                List(Shown(rows(), emptyMessage));
            }

            if (foregrounds.Count > 0 && emptyMessage.Length == 0)
            {
                View.SelectedItem = selectedIndex;
            }
        }
        finally
        {
            showing = false;
        }
    }

    /// <summary>The panel moves the selection itself while it shows its rows,
    /// and an empty panel's message is not a row to choose.</summary>
    private void ReportChosenRow()
    {
        if (showing || listedMessage.Length > 0 || View.SelectedItem is not { } row)
        {
            return;
        }

        RowChosen?.Invoke(row);
    }

    public void Place(PanelArea area)
    {
        View.X = area.X;
        View.Y = area.Y;
        View.Width = area.Width;
        View.Height = area.Height;
        View.Visible = area != PanelLayout.Hidden;
        frame.Place(area);
    }

    private static IReadOnlyList<ListRow> Shown(IReadOnlyList<ListRow> rows, string emptyMessage) =>
        rows.Count == 0 && emptyMessage.Length > 0
            ? [new ListRow(emptyMessage, Color.DarkGray)]
            : rows;

    private void List(IReadOnlyList<ListRow> shown)
    {
        foregrounds = [.. shown.Select(row => row.Foreground)];
        View.SetSource(new ObservableCollection<string>(shown.Select(row => row.Text)));
    }

    private void ColorRow(ListViewRowEventArgs args)
    {
        if (args.Row >= foregrounds.Count || foregrounds[args.Row] is not { } foreground)
        {
            return;
        }

        var role = View.IsSelectedOrMarked(args.Row) && View.HasFocus ? VisualRole.Focus : VisualRole.Normal;
        args.RowAttribute = new global::Terminal.Gui.Drawing.Attribute(
            foreground,
            View.GetAttributeForRole(role).Background);
    }
}
