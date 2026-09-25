namespace TerminalDotnet.Files;

/// <summary>
/// Writes a whole file by creating a fresh one beside it and renaming that
/// into place. A rename replaces a symbolic link standing at the path instead
/// of following it, so a link planted there cannot redirect the write
/// somewhere else, and nobody ever reads a half-written file.
/// </summary>
public static class FileReplacement
{
    public static async Task WriteAsync(
        string path,
        string text,
        CancellationToken cancellationToken = default)
    {
        var written = SiblingOf(path);
        try
        {
            await WriteNewFileAsync(written, text, cancellationToken).ConfigureAwait(false);
            File.Move(written, path, overwrite: true);
        }
        catch
        {
            DeleteQuietly(written);
            throw;
        }
    }

    private static string SiblingOf(string path) => Path.Combine(
        Path.GetDirectoryName(Path.GetFullPath(path))!,
        $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");

    /// <summary>Creating the file, rather than opening one, never follows a
    /// link someone left under the same name.</summary>
    private static async Task WriteNewFileAsync(
        string path,
        string text,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(text.AsMemory(), cancellationToken).ConfigureAwait(false);
    }

    private static void DeleteQuietly(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }
}
