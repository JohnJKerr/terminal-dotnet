using TerminalDotnet.Flags;

namespace TerminalDotnet.Terminal;

public sealed record FlagPanelRow(string Text, FileRowTone Tone);

public sealed record FlagPanelSnapshot(
    IReadOnlyList<Flag> Flags,
    int SelectedIndex,
    string SearchQuery,
    IReadOnlyList<FilterChip> Filters,
    string EmptyMessage)
{
    public IReadOnlyList<FlagPanelRow> Rows => RowsFrom(Flags);

    public int SelectedRowIndex => RowsBefore(Flags, SelectedIndex);

    public static FlagPanelSnapshot From(FlagState state) => new(
        state.Flags,
        state.SelectedIndex,
        state.SearchQuery,
        FlagFilters.Chips(state.ActiveFilter),
        state.Loading ? "" : EmptyMessageFrom(state));

    private static string EmptyMessageFrom(FlagState state)
    {
        if (state.Flags.Count > 0)
        {
            return "";
        }
        var subject = state.ActiveFilter is null
            ? "flags"
            : $"{state.ActiveFilter.Value.ToString().ToLowerInvariant()} flags";
        return state.SearchQuery.Length == 0
            ? $"No {subject} to show"
            : $"No {subject} match '{state.SearchQuery}'";
    }

    private static IReadOnlyList<FlagPanelRow> RowsFrom(IReadOnlyList<Flag> flags)
    {
        var rows = new List<FlagPanelRow>();
        string? heading = null;
        foreach (var flag in flags)
        {
            if (heading != flag.Heading)
            {
                heading = flag.Heading;
                rows.Add(new FlagPanelRow(flag.Heading, FileRowTone.Neutral));
            }
            rows.Add(new FlagPanelRow($"  {flag.DisplayPath}:{flag.Line}", FileRowTone.Neutral));
            rows.Add(new FlagPanelRow($"    {flag.Comment}", FileRowTone.Neutral));
        }
        return rows;
    }

    private static int RowsBefore(IReadOnlyList<Flag> flags, int selectedIndex)
    {
        var preceding = flags.Take(selectedIndex).ToArray();
        var headings = preceding.Select(flag => flag.Heading).Distinct().Count();
        var selectedStartsAHeading = selectedIndex < flags.Count &&
            (selectedIndex == 0 || flags[selectedIndex - 1].Heading != flags[selectedIndex].Heading);
        return preceding.Length * 2 + headings + (selectedStartsAHeading ? 1 : 0);
    }
}

public static class FlagFilters
{
    private static readonly FlagCategory[] Offered = Enum.GetValues<FlagCategory>();

    public static IReadOnlyList<FilterChip> Chips(FlagCategory? active) => Offered
        .Select((category, index) => new FilterChip($"{index + 1}. {category}", category == active))
        .ToArray();

    public static FlagCategory? Numbered(int number) => number >= 1 && number <= Offered.Length
        ? Offered[number - 1]
        : null;
}
