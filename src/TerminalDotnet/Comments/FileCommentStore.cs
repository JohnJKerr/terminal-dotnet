namespace TerminalDotnet.Comments;

/// <summary>
/// Keeps the comments in a file on disk. The reader names the file, so it may
/// sit somewhere that cannot be written to, which is reported rather than
/// thrown.
///
/// The suggested file sits in the repository being read, which anyone could
/// have committed a symbolic link into. The text is therefore written to a
/// fresh file beside it and renamed into place: a rename replaces the link
/// itself, where writing through the path would overwrite whatever the link
/// points at.
/// </summary>
public sealed class FileCommentStore : ICommentStore
{
    public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default) =>
        Task.FromResult(File.Exists(path));

    public async Task<bool> TryWriteAsync(
        string path,
        string text,
        CancellationToken cancellationToken = default)
    {
        var written = "";
        try
        {
            written = SiblingOf(path);
            await WriteNewFileAsync(written, text, cancellationToken);
            File.Move(written, path, overwrite: true);
            return true;
        }
        catch (Exception exception) when (exception is IOException
            or UnauthorizedAccessException
            or NotSupportedException
            or ArgumentException)
        {
            DeleteQuietly(written);
            return false;
        }
    }

    private static string SiblingOf(string path) => Path.Combine(
        Path.GetDirectoryName(Path.GetFullPath(path))!,
        $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");

    private static async Task WriteNewFileAsync(
        string path,
        string text,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(text.AsMemory(), cancellationToken);
    }

    private static void DeleteQuietly(string path)
    {
        try
        {
            if (path.Length > 0)
            {
                File.Delete(path);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }
}
