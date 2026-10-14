namespace TerminalDotnet.Files;

public static class WorkspacePath
{
    public static string? RelativeTo(string folder, string path)
    {
        try
        {
            return Path.GetRelativePath(folder, path);
        }
        catch (Exception exception) when (CannotPlace(exception))
        {
            return null;
        }
    }

    private static bool CannotPlace(Exception exception) =>
        exception is ArgumentException or PathTooLongException;
}
