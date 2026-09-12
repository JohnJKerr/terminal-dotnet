using System.Collections.ObjectModel;

namespace TerminalDotnet;

internal static class Snapshot
{
    public static IReadOnlyList<T> Of<T>(IEnumerable<T> items) =>
        new ReadOnlyCollection<T>([.. items]);
}
