namespace TerminalDotnet.Terminal;

/// <summary>
/// The letter that reaches each panel. Two panels begin with the same letter,
/// so the keys are named here rather than read off the panel's own name.
/// </summary>
public static class PanelKeys
{
    public static string For(PanelKind panel) => panel switch
    {
        PanelKind.Explorer => "E",
        PanelKind.Tests => "T",
        PanelKind.Issues => "I",
        PanelKind.Changes => "G",
        _ => "C"
    };

    public static PanelKind? For(string key) => Enum
        .GetValues<PanelKind>()
        .Cast<PanelKind?>()
        .FirstOrDefault(panel => For(panel!.Value) == key);
}
