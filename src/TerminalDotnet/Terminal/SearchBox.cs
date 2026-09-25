namespace TerminalDotnet.Terminal;

/// <summary>The box beneath the panels that searches the one taking the
/// keys, titled with how much its search found.</summary>
public static class SearchBox
{
    public static string Title(string query, int hitCount) => query.Length == 0
        ? "Search"
        : $"Search — {CountedNoun.Of(hitCount, "hit")}";
}
