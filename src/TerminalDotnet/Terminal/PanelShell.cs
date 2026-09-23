namespace TerminalDotnet.Terminal;

public enum PanelKind
{
    Explorer,
    Tests,
    Changes,
    Issues,
    Comments
}

public sealed record PanelLabel(string Key, string Name);

public sealed record PanelCounts(int? Issues, int? Comments);

public sealed record PanelShellState(IReadOnlyList<string> Panels, PanelKind ActivePanel)
{
    /// <summary>The Explorer lists the projects' files until the reader asks
    /// for every file beneath the launch folder.</summary>
    public bool ShowsAllFiles { get; init; }

    /// <summary>The list on the left the reader was last in, which stretches
    /// to show more of its rows while they work elsewhere.</summary>
    public PanelKind ExpandedList { get; init; } = PanelKind.Explorer;

    public int ActiveIndex => (int)ActivePanel;

    public IReadOnlyList<PanelLabel> KeyedPanels =>
        [.. Panels.Select((name, index) => new PanelLabel(PanelKeys.For((PanelKind)index), name))];

    public IReadOnlyList<PanelLabel> KeyedPanelsWith(PanelCounts counts) =>
        [.. KeyedPanels.Select(panel => panel with { Name = CountedName(panel.Name, counts) })];

    private static string CountedName(string name, PanelCounts counts) => name switch
    {
        "Issues" => $"Issues ({Shown(counts.Issues)})",
        "Comments" => $"Comments ({Shown(counts.Comments)})",
        _ => name
    };

    private static string Shown(int? count) => count?.ToString() ?? "-";
}

public sealed class PanelShell
{
    public PanelShellState State { get; private set; } =
        new(["Explorer", "Tests", "Changes", "Issues", "Comments"], PanelKind.Explorer);

    public void Select(int index)
    {
        var panel = (PanelKind)Math.Clamp(index, 0, State.Panels.Count - 1);
        State = State with
        {
            ActivePanel = panel,
            ExpandedList = panel is PanelKind.Explorer or PanelKind.Tests or PanelKind.Changes
                ? panel
                : State.ExpandedList
        };
    }

    public void ToggleAllFiles() => State = State with { ShowsAllFiles = !State.ShowsAllFiles };

    public void SelectPrevious() => Select(Wrapped(State.ActiveIndex - 1));

    public void SelectNext() => Select(Wrapped(State.ActiveIndex + 1));

    private int Wrapped(int index) => (index + State.Panels.Count) % State.Panels.Count;
}
