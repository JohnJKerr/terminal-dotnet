using System.IO.Enumeration;

namespace TerminalDotnet.Files;

internal static class SourceTree
{
    /// <summary>Neither the compiler's output nor git's own store is
    /// something the reader browses.</summary>
    private static readonly string[] SkippedDirectories = ["bin", "obj", ".git"];

    /// <summary>A folder that cannot be read is passed over rather than
    /// ending the walk, so the rest of the tree is still listed.</summary>
    private static readonly EnumerationOptions Walk = new()
    {
        RecurseSubdirectories = true,
        IgnoreInaccessible = true,
        AttributesToSkip = FileAttributes.None
    };

    public static IEnumerable<string> FilesUnder(string root, string searchPattern) =>
        new FileSystemEnumerable<string>(root, (ref FileSystemEntry entry) => entry.ToFullPath(), Walk)
        {
            ShouldIncludePredicate = (ref FileSystemEntry entry) =>
                !entry.IsDirectory && FileSystemName.MatchesSimpleExpression(searchPattern, entry.FileName),
            ShouldRecursePredicate = (ref FileSystemEntry entry) =>
                !IsSkipped(entry.FileName) && !LinksElsewhere(entry.Attributes)
        };

    /// <summary>A folder that links to another is left to the walk of wherever
    /// it really lives: following the link would list those files a second
    /// time, and a link pointing back at a folder above it would walk until
    /// the path itself became too long to resolve.</summary>
    private static bool LinksElsewhere(FileAttributes attributes) =>
        attributes.HasFlag(FileAttributes.ReparsePoint);

    private static bool IsSkipped(ReadOnlySpan<char> directoryName)
    {
        foreach (var skipped in SkippedDirectories)
        {
            if (directoryName.Equals(skipped, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
