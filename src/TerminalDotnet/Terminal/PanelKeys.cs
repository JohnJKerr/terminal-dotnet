namespace TerminalDotnet.Terminal;

/// <summary>
/// The number that reaches each panel, as its title shows beside its name.
/// </summary>
public static class PanelKeys
{
    public static string For(PanelKind panel) => ((int)panel).ToString();

    public static PanelKind? For(string key) => Enum
        .GetValues<PanelKind>()
        .Cast<PanelKind?>()
        .FirstOrDefault(panel => For(panel!.Value) == key);
}
