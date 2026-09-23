namespace TerminalDotnet.Terminal;

public enum PanelKind
{
    Preview,
    Explorer,
    Tests,
    Changes,
    Issues,
    Comments
}

public sealed record PanelShellState(IReadOnlyList<string> Panels, PanelKind ActivePanel)
{
    /// <summary>The Explorer lists the projects' files until the reader asks
    /// for every file beneath the launch folder.</summary>
    public bool ShowsAllFiles { get; init; }

    /// <summary>The list on the left the reader was last in, which stretches
    /// to show more of its rows while they work elsewhere.</summary>
    public PanelKind ExpandedList { get; init; } = PanelKind.Explorer;

    /// <summary>The list whose selection the preview shows. Moving into the
    /// preview keeps it on the list the reader came from.</summary>
    public PanelKind PreviewedList { get; init; } = PanelKind.Explorer;

    /// <summary>Tab walks the lists on the left, then the preview beside
    /// them, then the panels beneath it.</summary>
    private static readonly PanelKind[] TabOrder =
    [
        PanelKind.Explorer, PanelKind.Tests, PanelKind.Changes,
        PanelKind.Preview, PanelKind.Issues, PanelKind.Comments
    ];

    public PanelKind Stepped(int step)
    {
        var index = Array.IndexOf(TabOrder, ActivePanel) + step;
        return TabOrder[(index % TabOrder.Length + TabOrder.Length) % TabOrder.Length];
    }

    public int ActiveIndex => (int)ActivePanel;
}

public sealed class PanelShell
{
    public PanelShellState State { get; private set; } =
        new(["Preview", "Explorer", "Tests", "Changes", "Issues", "Comments"], PanelKind.Explorer);

    public void Select(PanelKind panel)
    {
        State = State with
        {
            ActivePanel = panel,
            ExpandedList = panel is PanelKind.Explorer or PanelKind.Tests or PanelKind.Changes
                ? panel
                : State.ExpandedList,
            PreviewedList = panel == PanelKind.Preview ? State.PreviewedList : panel
        };
    }

    public void ToggleAllFiles() => State = State with { ShowsAllFiles = !State.ShowsAllFiles };

    public void SelectPrevious() => Select(State.Stepped(-1));

    public void SelectNext() => Select(State.Stepped(1));
}
