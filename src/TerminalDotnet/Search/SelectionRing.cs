namespace TerminalDotnet.Search;

public static class SelectionRing
{
    public const int None = -1;

    public static int Next(IReadOnlyList<int> indices, int selectedIndex) => indices.FirstOrDefault(
        index => index > selectedIndex,
        indices.FirstOrDefault(None));
}
