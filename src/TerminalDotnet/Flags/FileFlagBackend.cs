using System.Text.RegularExpressions;
using TerminalDotnet.Files;

namespace TerminalDotnet.Flags;

public sealed partial class FileFlagBackend(IFileExplorerBackend files) : IFlagBackend
{
    public async Task<IReadOnlyList<Flag>> DiscoverAsync(
        string target,
        CancellationToken cancellationToken = default)
    {
        var root = Path.GetDirectoryName(Path.GetFullPath(target))!;
        var discovered = await files.DiscoverAsync(target, cancellationToken);
        var flags = discovered
            .Where(file => file.GitStatus != FileGitStatus.Deleted)
            .SelectMany(file => FlagsIn(file.Path, root))
            .ToArray();
        return flags;
    }

    private static IEnumerable<Flag> FlagsIn(string path, string root)
    {
        try
        {
            return (FileText.ReadLinesWithin(path) ?? [])
                .Select((line, index) => FlagIn(path, root, index + 1, line))
                .Where(flag => flag is not null)
                .Cast<Flag>()
                .ToArray();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    private static Flag? FlagIn(string path, string root, int lineNumber, string line)
    {
        var match = FlagPattern().Match(line);
        if (!match.Success || !Enum.TryParse<FlagKind>(match.Groups["kind"].Value, true, out var kind))
        {
            return null;
        }

        var comment = match.Groups["comment"].Value.Trim();
        if (comment.EndsWith("*/", StringComparison.Ordinal))
        {
            comment = comment[..^2].TrimEnd();
        }

        return new Flag(
            Path.GetFullPath(path),
            Path.GetRelativePath(root, path),
            lineNumber,
            kind,
            comment);
    }

    [GeneratedRegex(@"(?://|/\*+|^\s*\*)\s*(?<kind>todo|fixme|review|question|note|warning|warn|hack|xxx|bug|deprecated|refactor|optimize)\b[\s:;\-]*(?<comment>.*)$", RegexOptions.IgnoreCase)]
    private static partial Regex FlagPattern();
}
