namespace TerminalDotnet.Terminal;

public enum PanelKind
{
    Explorer,
    Tests
}

public sealed record PanelShellState(IReadOnlyList<string> Panels, PanelKind ActivePanel);

public sealed class PanelShell
{
    public PanelShellState State { get; private set; } = new(["Explorer", "Tests"], PanelKind.Explorer);

    public void Select(int index)
    {
        State = State with { ActivePanel = index == 0 ? PanelKind.Explorer : PanelKind.Tests };
    }
}
