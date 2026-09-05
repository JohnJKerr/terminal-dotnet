using Terminal.Gui.Drawing;
using TerminalDotnet.Filters;

namespace TerminalDotnet.Terminal;

public sealed record FilterChip(string Text, bool IsActive);

public static class PanelFilters
{
    private static readonly ExplorerFilter[] Offered = Enum.GetValues<ExplorerFilter>();

    public static IReadOnlyList<FilterChip> Chips(ExplorerFilter? active) => Offered
        .Select((filter, index) => new FilterChip($"{index + 1}. {filter}", filter == active))
        .ToArray();

    public static ExplorerFilter? Numbered(int number) => number >= 1 && number <= Offered.Length
        ? Offered[number - 1]
        : null;
}

public static class FilterAppearance
{
    public static Color ForegroundFor(bool isActive) => isActive ? Color.BrightGreen : Color.Gray;
}
