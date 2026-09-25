namespace TerminalDotnet.Terminal;

/// <summary>A piece of a panel's title, drawn in the filter colour when it
/// names a filter in use.</summary>
public sealed record TitleSegment(string Text, bool IsActive);

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
        bool focused) =>
        string.Join(" ", Segments(panel, filters, searchQuery, focused).Select(segment => segment.Text));

    public static IReadOnlyList<TitleSegment> Segments(
        PanelKind panel,
        IReadOnlyList<FilterChip> filters,
        string searchQuery,
        bool focused) =>
    [
        new($"[{PanelKeys.For(panel)}]─{panel}", false),
        .. filters.Select(chip => new TitleSegment(ChipText(chip, focused), chip.IsActive)),
        .. searchQuery.Length == 0 ? Array.Empty<TitleSegment>() : [new($"─ /{searchQuery}", false)]
    ];

    /// <summary>The segments that fit in <paramref name="room"/> columns, each
    /// drawn after a space. The panel's name is cut short rather than lost, so
    /// a narrow panel still says what it shows; a filter with no room is left
    /// off, along with everything after it.</summary>
    public static IReadOnlyList<TitleSegment> Fitted(IReadOnlyList<TitleSegment> segments, int room)
    {
        var fitted = new List<TitleSegment>();
        var used = 0;
        foreach (var segment in segments)
        {
            var needed = segment.Text.Length + 1;
            if (used + needed <= room)
            {
                fitted.Add(segment);
                used += needed;
                continue;
            }

            if (fitted.Count == 0 && room >= 3)
            {
                fitted.Add(segment with { Text = $"{segment.Text[..(room - 2)]}…" });
            }

            break;
        }

        return fitted;
    }

    public static string Footer(int selectedIndex, int rowCount) => rowCount == 0
        ? "0 of 0"
        : $"{selectedIndex + 1} of {rowCount}";

    /// <summary>A filter in use is named on every panel, so the reader can
    /// tell what a panel is hiding without moving to it.</summary>
    private static string ChipText(FilterChip chip, bool focused) =>
        focused || chip.IsActive ? chip.Text : KeyOf(chip);

    private static string KeyOf(FilterChip chip) => chip.Text.Split(' ')[0];
}
