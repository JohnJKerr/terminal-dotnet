using System.Collections;
using System.Collections.Specialized;
using Terminal.Gui.Views;
using Attribute = Terminal.Gui.Drawing.Attribute;
using TextStyle = Terminal.Gui.Drawing.TextStyle;

namespace TerminalDotnet.Terminal;

internal sealed class PanelListSource(IReadOnlyList<PanelLabel> initialPanels) : IListDataSource
{
    private IReadOnlyList<PanelLabel> panels = initialPanels;

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public int Count => panels.Count;

    public int MaxItemLength => panels.Max(panel => TextFor(panel).Length);

    public bool SuspendCollectionChangedEvent { get; set; }

    public void Update(IReadOnlyList<PanelLabel> updatedPanels)
    {
        panels = updatedPanels;
        if (!SuspendCollectionChangedEvent)
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }

    public void Render(
        ListView listView,
        bool selected,
        int item,
        int col,
        int row,
        int width,
        int viewportX)
    {
        var panel = panels[item];
        var normal = listView.GetCurrentAttribute();
        listView.Move(col, row);
        listView.SetAttribute(Bolded(normal));
        listView.AddStr(panel.Key);
        listView.SetAttribute(normal);
        listView.AddStr(Padded($" {panel.Name}", width - panel.Key.Length));
    }

    public bool IsMarked(int item) => false;

    public void SetMark(int item, bool value)
    {
    }

    public void RenderMark(ListView listView, int item, int col, int row, bool selected)
    {
    }

    public IList ToList() => panels.Select(TextFor).ToList();

    public void Dispose()
    {
    }

    private static Attribute Bolded(Attribute attribute) =>
        new(attribute.Foreground, attribute.Background, attribute.Style | TextStyle.Bold);

    private static string Padded(string text, int width) =>
        width <= 0 ? "" : text.Length >= width ? text[..width] : text.PadRight(width);

    private static string TextFor(PanelLabel panel) => $"{panel.Key} {panel.Name}";
}
