namespace TerminalDotnet.Terminal;

public enum PanelKind
{
    Explorer,
    Tests,
    Changes
}

public sealed record PanelShellState(IReadOnlyList<string> Panels, PanelKind ActivePanel)
{
    public int ActiveIndex => (int)ActivePanel;

    /// <summary>The panel names as the reader sees them, each carrying the
    /// number that targets it.</summary>
    public IReadOnlyList<string> NumberedPanels =>
        [.. Panels.Select((name, index) => $"{index + 1}. {name}")];
}

public sealed class PanelShell
{
    public PanelShellState State { get; private set; } =
        new(["Explorer", "Tests", "Changes"], PanelKind.Explorer);

    public void Select(int index)
    {
        State = State with
        {
            ActivePanel = (PanelKind)Math.Clamp(index, 0, State.Panels.Count - 1)
        };
    }

    public void SelectPrevious() => Select(Wrapped(State.ActiveIndex - 1));

    public void SelectNext() => Select(Wrapped(State.ActiveIndex + 1));

    public void SelectNumbered(int number)
    {
        if (number < 1 || number > State.Panels.Count)
        {
            return;
        }

        Select(number - 1);
    }

    private int Wrapped(int index) => (index + State.Panels.Count) % State.Panels.Count;
}
