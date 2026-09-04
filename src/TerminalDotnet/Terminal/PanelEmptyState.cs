namespace TerminalDotnet.Terminal;

public static class PanelEmptyState
{
    public static string For(string subject, int rowCount, string searchQuery)
    {
        if (rowCount > 0)
        {
            return "";
        }

        return searchQuery.Length == 0
            ? $"No {subject} to show"
            : $"No {subject} match '{searchQuery}'";
    }
}
