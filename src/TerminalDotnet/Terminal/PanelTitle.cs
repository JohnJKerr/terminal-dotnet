namespace TerminalDotnet.Terminal;

/// <summary>
/// The words along a panel's frame. The top edge names the panel, the key that
/// reaches it, its filters and its search; the bottom edge says where the
/// selection stands. Only the focused panel spells out every filter, because
/// the others share the screen with it.
/// </summary>
public static class PanelTitle
{
    public static string For(
        PanelKind panel,
        IReadOnlyList<FilterChip> filters,
        string searchQuery,
        bool focused) => string.Join(
        " ",
        [
            $"[{PanelKeys.For(panel)}]─{panel}",
            .. filters.Select(chip => ChipText(chip, focused)),
            .. searchQuery.Length == 0 ? Array.Empty<string>() : [$"─ /{searchQuery}"]
        ]);

    public static string Footer(int selectedIndex, int rowCount) => rowCount == 0
        ? "0 of 0"
        : $"{selectedIndex + 1} of {rowCount}";

    /// <summary>A filter in use is marked and named on every panel, so the
    /// reader can tell what a panel is hiding without moving to it.</summary>
    private static string ChipText(FilterChip chip, bool focused) => chip.IsActive
        ? $"{ActiveMark}{chip.Text}"
        : focused ? chip.Text : KeyOf(chip);

    private const string ActiveMark = "●";

    private static string KeyOf(FilterChip chip) => chip.Text.Split(' ')[0];
}
