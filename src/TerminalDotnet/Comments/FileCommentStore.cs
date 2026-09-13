namespace TerminalDotnet.Comments;

/// <summary>
/// Keeps the comments in a file on disk. The reader names the file, so it may
/// sit somewhere that cannot be written to, which is reported rather than
/// thrown.
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
        try
        {
            await File.WriteAllTextAsync(path, text, cancellationToken);
            return true;
        }
        catch (Exception exception) when (exception is IOException
            or UnauthorizedAccessException
            or NotSupportedException
            or ArgumentException)
        {
            return false;
        }
    }
}
