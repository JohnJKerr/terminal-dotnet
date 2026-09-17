namespace TerminalDotnet;

/// <summary>
/// A count and the thing counted, as a reader would say it: one error, two
/// errors. Status lines and toasts report counts that are often one, and
/// "1 Errors" reads as a bug in the app rather than a count.
/// </summary>
public static class CountedNoun
{
    public static string Of(int count, string noun) =>
        count == 1 ? $"1 {noun}" : $"{count} {noun}s";
}
