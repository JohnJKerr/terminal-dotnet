using TerminalDotnet.Filters;

namespace TerminalDotnet.Terminal;

public static class PanelEmptyState
{
    public static string For(
        string subject,
        int rowCount,
        string searchQuery,
        ExplorerFilter? activeFilter = null)
    {
        if (rowCount > 0)
        {
            return "";
        }

        var rows = Described(subject, activeFilter);
        return searchQuery.Length == 0
            ? $"No {rows} to show"
            : $"No {rows} match '{searchQuery}'";
    }

    private static string Described(string subject, ExplorerFilter? activeFilter) =>
        activeFilter is null
            ? subject
            : $"{activeFilter.Value.ToString().ToLowerInvariant()} {subject}";
}
