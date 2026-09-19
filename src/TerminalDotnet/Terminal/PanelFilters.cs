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
        .Select((filter, index) => new FilterChip(
            $"{index + 1}. {filter.DisplayName()}",
            filter == active))
        .ToArray();

    public static ExplorerFilter? Numbered(int number) => Numbered(number, FileFilters);

    public static ExplorerFilter? NumberedTest(int number) => Numbered(number, TestFilters);

    private static ExplorerFilter? Numbered(int number, IReadOnlyList<ExplorerFilter> offered) =>
        number >= 1 && number <= offered.Count
        ? offered[number - 1]
        : null;
}

public static class FilterAppearance
{
    public static Color ForegroundFor(bool isActive) => isActive ? Color.BrightGreen : Color.Gray;
}
