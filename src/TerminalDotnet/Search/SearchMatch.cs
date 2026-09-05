namespace TerminalDotnet.Search;

public static class SearchMatch
{
    public static bool Matches(string candidate, string query) =>
        candidate.Contains(query, StringComparison.OrdinalIgnoreCase);
}
