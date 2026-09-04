namespace TerminalDotnet.Search;

public static class FuzzyMatch
{
    public static bool Matches(string candidate, string query)
    {
        var queryIndex = 0;
        foreach (var character in candidate)
        {
            if (queryIndex < query.Length &&
                char.ToUpperInvariant(character) == char.ToUpperInvariant(query[queryIndex]))
            {
                queryIndex++;
            }
        }

        return queryIndex == query.Length;
    }
}
