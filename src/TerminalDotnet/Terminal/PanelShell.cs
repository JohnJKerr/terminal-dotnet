namespace TerminalDotnet.Terminal;

public enum PanelKind
{
    Explorer,
    Tests
}

public sealed record PanelShellState(IReadOnlyList<string> Panels, PanelKind ActivePanel);

public sealed class PanelShell
{
    public PanelShellState State { get; } = new(["Explorer", "Tests"], PanelKind.Explorer);
}
