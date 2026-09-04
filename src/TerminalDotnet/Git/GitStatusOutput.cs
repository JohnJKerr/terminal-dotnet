namespace TerminalDotnet.Git;

public enum GitChangeKind
{
    Added,
    Modified,
    Deleted
}

public sealed record GitStatusEntry(string RelativePath, GitChangeKind Kind);

public static class GitStatusOutput
{
    private const string RenameArrow = " -> ";
    private const int PathStart = 3;

    public static IReadOnlyList<GitStatusEntry> EntriesFrom(string standardOutput) => standardOutput
        .ReplaceLineEndings("\n")
        .Split('\n', StringSplitOptions.RemoveEmptyEntries)
        .Where(line => line.Length > PathStart)
        .Select(EntryFrom)
        .ToArray();

    private static GitStatusEntry EntryFrom(string line) => new(
        RelativePathFrom(line[PathStart..]),
        KindFrom(line[..2]));

    private static string RelativePathFrom(string path)
    {
        var rename = path.IndexOf(RenameArrow, StringComparison.Ordinal);
        return rename < 0 ? path : path[(rename + RenameArrow.Length)..];
    }

    private static GitChangeKind KindFrom(string code)
    {
        if (code.Contains('D', StringComparison.Ordinal))
        {
            return GitChangeKind.Deleted;
        }

        return code == "??" || code.Contains('A', StringComparison.Ordinal)
            ? GitChangeKind.Added
            : GitChangeKind.Modified;
    }
}
