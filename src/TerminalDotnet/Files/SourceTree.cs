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
        if (IsSkipped(directory))
        {
            return;
        }

        pending.Push(directory);
    }

    private static bool IsSkipped(string directory) =>
        SkippedDirectories.Contains(Path.GetFileName(directory), StringComparer.OrdinalIgnoreCase);
}
