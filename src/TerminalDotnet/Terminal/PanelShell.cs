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
}
