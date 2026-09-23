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
            .. filters.Select(chip => focused || chip.IsActive ? chip.Text : KeyOf(chip)),
            .. searchQuery.Length == 0 ? Array.Empty<string>() : [$"─ /{searchQuery}"]
        ]);

    public static string Footer(int selectedIndex, int rowCount) => rowCount == 0
        ? "0 of 0"
        : $"{selectedIndex + 1} of {rowCount}";

    private static string KeyOf(FilterChip chip) => chip.Text.Split(' ')[0];
}
