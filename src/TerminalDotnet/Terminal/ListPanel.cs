using System.Collections.ObjectModel;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace TerminalDotnet.Terminal;

/// <summary>A row of a list panel, drawn in its own colour or, when it has
/// none, in the panel's.</summary>
internal sealed record ListRow(string Text, Color? Foreground)
{
    public static ListRow Toned(string text, FileRowTone tone) => new(
        text,
        tone == FileRowTone.Neutral ? null : FileRowAppearance.ForegroundFor(tone, Color.White));
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
    private PanelArea area = new(0, 0, 0, 0);

    public ListPanel()
    {
        View = new ListView
        {
            BorderStyle = LineStyle.Single,
            ShowMarks = false,
            KeystrokeNavigator = null
        };
        View.RowRender += (_, args) => ColorRow(args);
        Footer = new Label { Height = 1, HotKeySpecifier = new System.Text.Rune(0xFFFF) };
    }

    public ListView View { get; }

    /// <summary>Where the selection stands, drawn over the bottom edge of the
    /// frame. It sits beside the list rather than in it, because a list draws
    /// nothing on its own border.</summary>
    public Label Footer { get; }

    /// <summary>A panel with nothing to list says why in place of its rows,
    /// and has no row to select. A message such as the discovery marker moves
    /// while the content stays the same, so either one changing lists again.
    /// </summary>
    public void Show(
        string title,
        string footer,
        object content,
        Func<IReadOnlyList<ListRow>> rows,
        int selectedIndex,
        string emptyMessage)
    {
        View.Title = title;
        Footer.Text = footer;
        PlaceFooter();
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

    public void Place(PanelArea placed)
    {
        area = placed;
        View.X = area.X;
        View.Y = area.Y;
        View.Width = area.Width;
        View.Height = area.Height;
        PlaceFooter();
    }

    private void PlaceFooter()
    {
        var width = Footer.Text.Length;
        Footer.Width = width;
        Footer.X = Math.Max(area.X, area.X + area.Width - width - 2);
        Footer.Y = area.Y + area.Height - 1;
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
