namespace TerminalDotnet.Files;

internal static class ProjectTree
{
    private static readonly string[] BuildDirectories = ["bin", "obj"];

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
        if (IsBuildOutput(directory))
        {
            return;
        }

        pending.Push(directory);
    }

    private static bool IsBuildOutput(string directory) =>
        BuildDirectories.Contains(Path.GetFileName(directory), StringComparer.OrdinalIgnoreCase);
}
