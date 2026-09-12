namespace TerminalDotnet.Git;

public enum GitChangeKind
{
    Added,
    Modified,
    Deleted
}

public sealed record GitStatusEntry(string RelativePath, GitChangeKind Kind)
{
    public GitChangeKind? Staged { get; init; }

    public GitChangeKind? Unstaged { get; init; }
}

public static class GitStatusOutput
{
    private const int PathStart = 3;

    public static IReadOnlyList<GitStatusEntry> EntriesFrom(string standardOutput) =>
        ChangeRecords(standardOutput.Split('\0', StringSplitOptions.RemoveEmptyEntries))
            .Select(EntryFrom)
            .ToArray();

    private static IEnumerable<string> ChangeRecords(IReadOnlyList<string> records)
    {
        var index = 0;
        while (index < records.Count)
        {
            var record = records[index++];
            if (record.Length <= PathStart)
            {
                continue;
            }

            yield return record;
            index += RenamesAPath(record) ? 1 : 0;
        }
    }

    private static bool RenamesAPath(string record) =>
        record[0] is 'R' or 'C' || record[1] is 'R' or 'C';

    private static GitStatusEntry EntryFrom(string record) => new(
        record[PathStart..],
        KindFrom(record[..2]))
    {
        Staged = StagedKindFrom(record[0]),
        Unstaged = UnstagedKindFrom(record[1])
    };

    private static GitChangeKind? StagedKindFrom(char code) =>
        code == '?' ? null : TrackedKindFrom(code);

    private static GitChangeKind? UnstagedKindFrom(char code) =>
        code == '?' ? GitChangeKind.Added : TrackedKindFrom(code);

    private static GitChangeKind? TrackedKindFrom(char code) => code switch
    {
        ' ' => null,
        'A' => GitChangeKind.Added,
        'D' => GitChangeKind.Deleted,
        _ => GitChangeKind.Modified
    };

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
