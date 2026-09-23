using Terminal.Gui.Drawing;
using TerminalDotnet.Filters;

namespace TerminalDotnet.Terminal;

public sealed record FilterChip(string Text, bool IsActive);

public static class PanelFilters
{
    private static readonly ExplorerFilter[] FileFilters = [ExplorerFilter.Updated];
    private static readonly ExplorerFilter[] TestFilters = Enum.GetValues<ExplorerFilter>();

    public static IReadOnlyList<FilterChip> Chips(ExplorerFilter? active) => FileFilters
        .Select(filter => new FilterChip($"{filter.Key()} {filter.DisplayName()}", filter == active))
        .ToArray();

    public static IReadOnlyList<FilterChip> TestChips(ExplorerFilter? active) =>
        Chips(active, TestFilters);

    private static IReadOnlyList<FilterChip> Chips(
        ExplorerFilter? active,
        IReadOnlyList<ExplorerFilter> offered) => offered
        .Select((filter, index) => new FilterChip(
            $"{index + 1}. {filter.DisplayName()}",
            filter == active))
        .ToArray();

    public static ExplorerFilter? Lettered(string letter) => Lettered(letter, FileFilters);

    public static ExplorerFilter? LetteredTest(string letter) => Lettered(letter, TestFilters);

    private static ExplorerFilter? Lettered(string letter, IReadOnlyList<ExplorerFilter> offered) =>
        offered.Cast<ExplorerFilter?>().FirstOrDefault(filter => filter!.Value.Key() == letter);
}

public static class FilterAppearance
{
    public static Color ForegroundFor(bool isActive) => isActive ? Color.BrightGreen : Color.Gray;
}
