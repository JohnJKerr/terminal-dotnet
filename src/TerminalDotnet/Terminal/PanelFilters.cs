using Terminal.Gui.Drawing;
using TerminalDotnet.Filters;

namespace TerminalDotnet.Terminal;

public sealed record FilterChip(string Text, bool IsActive);

public static class PanelFilters
{
    private static readonly ExplorerFilter[] FileFilters = [ExplorerFilter.Updated];
    private static readonly ExplorerFilter[] TestFilters = Enum.GetValues<ExplorerFilter>();

    public static IReadOnlyList<FilterChip> Chips(ExplorerFilter? active) => Chips(active, FileFilters);

    public static IReadOnlyList<FilterChip> TestChips(ExplorerFilter? active) =>
        Chips(active, TestFilters);

    private static IReadOnlyList<FilterChip> Chips(
        ExplorerFilter? active,
        IReadOnlyList<ExplorerFilter> offered) => offered
        .Select(filter => new FilterChip(filter.DisplayName(), filter == active))
        .ToArray();

    /// <summary>The test filter to toggle so the panel takes a step along
    /// its filters. Stepping off either end toggles the filter in use, which
    /// turns it off and returns the panel to every test.</summary>
    public static ExplorerFilter TestToggleFor(ExplorerFilter? active, int step) =>
        FilterRing.Stepped(TestFilters, active, step) ?? active ?? TestFilters[0];
}

/// <summary>
/// A panel's filters in the order [ and ] step through them. The unfiltered
/// list stands between the last filter and the first, so the steps go round.
/// </summary>
public static class FilterRing
{
    public static T? Stepped<T>(T[] offered, T? active, int step)
        where T : struct
    {
        var size = offered.Length + 1;
        var standing = active is { } filter ? Array.IndexOf(offered, filter) + 1 : 0;
        var landed = ((standing + step) % size + size) % size;
        return landed == 0 ? null : offered[landed - 1];
    }
}

public static class FilterAppearance
{
    public static Color ForegroundFor(bool isActive) => isActive ? Color.BrightGreen : Color.Gray;
}
