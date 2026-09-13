namespace TerminalDotnet.Terminal;

public enum PanelKind
{
    Explorer,
    Files,
    Tests,
    Changes,
    Comments,
    Flags
}

public sealed record PanelLabel(string Key, string Name);

public sealed record PanelShellState(IReadOnlyList<string> Panels, PanelKind ActivePanel)
{
    public int ActiveIndex => (int)ActivePanel;

    public IReadOnlyList<PanelLabel> KeyedPanels =>
        [.. Panels.Select((name, index) => new PanelLabel(PanelKeys.For((PanelKind)index), name))];
}

public sealed class PanelShell
{
    public PanelShellState State { get; private set; } =
        new(["Explorer", "Files", "Tests", "Changes", "Comments", "Flags"], PanelKind.Explorer);

    public void Select(int index)
    {
        State = State with
        {
            ActivePanel = (PanelKind)Math.Clamp(index, 0, State.Panels.Count - 1)
        };
    }

    public void SelectPrevious() => Select(Wrapped(State.ActiveIndex - 1));

    public void SelectNext() => Select(Wrapped(State.ActiveIndex + 1));

    private int Wrapped(int index) => (index + State.Panels.Count) % State.Panels.Count;
}
