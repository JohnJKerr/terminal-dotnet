namespace TerminalDotnet.Terminal;

/// <summary>
/// The number that reaches each panel. The capital letters belong to the
/// panels' filters, so the panels are reached by the numbers beside them.
/// </summary>
public static class PanelKeys
{
    public static string For(PanelKind panel) => ((int)panel).ToString();

    public static PanelKind? For(string key) => Enum
        .GetValues<PanelKind>()
        .Cast<PanelKind?>()
        .FirstOrDefault(panel => For(panel!.Value) == key);
}
