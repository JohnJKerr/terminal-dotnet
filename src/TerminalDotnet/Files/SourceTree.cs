namespace TerminalDotnet.Files;

internal static class SourceTree
{
    /// <summary>Neither the compiler's output nor git's own store is
    /// something the reader browses.</summary>
    private static readonly string[] SkippedDirectories = ["bin", "obj", ".git"];

    public static IEnumerable<string> FilesUnder(string root, string searchPattern)
    {
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            foreach (var child in Directory.EnumerateDirectories(directory))
            {
                PushSourceDirectory(pending, child);
            }

            foreach (var file in Directory.EnumerateFiles(directory, searchPattern))
            {
                yield return file;
            }
        }
    }

    private static void PushSourceDirectory(Stack<string> pending, string directory)
    {
        if (IsSkipped(directory) || LinksElsewhere(directory))
        {
            return;
        }

        pending.Push(directory);
    }

    /// <summary>A folder that links to another is left to the walk of wherever
    /// it really lives: following the link would list those files a second
    /// time, and a link pointing back at a folder above it would walk until
    /// the path itself became too long to resolve. A folder that cannot be
    /// asked is treated the same way rather than walked.</summary>
    private static bool LinksElsewhere(string directory)
    {
        try
        {
            return new DirectoryInfo(directory).Attributes.HasFlag(FileAttributes.ReparsePoint);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return true;
        }
    }

    private static bool IsSkipped(string directory) =>
        SkippedDirectories.Contains(Path.GetFileName(directory), StringComparer.OrdinalIgnoreCase);
}
