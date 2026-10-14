namespace TerminalDotnet.Files;

/// <summary>
/// The path a watcher reports, relative to the folder it was asked to watch.
/// A junction or another drive root cannot be placed under that folder, and
/// throwing from the watcher's thread would take the process down with it.
/// </summary>
public static class WorkspacePath
{
    public static string? RelativeTo(string folder, string path)
    {
        try
        {
            return Path.GetRelativePath(folder, path);
        }
        catch (Exception exception) when (exception is ArgumentException or PathTooLongException)
        {
            return null;
        }
    }
}
