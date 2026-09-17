using TerminalDotnet.Files;

namespace TerminalDotnet.Comments;

/// <summary>
/// Keeps the comments in a file on disk. The reader names the file, so it may
/// sit somewhere that cannot be written to, which is reported rather than
/// thrown.
///
/// The suggested file sits in the repository being read, which anyone could
/// have committed a symbolic link into, so the file is replaced rather than
/// written through: whatever the link points at is left alone.
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
            await FileReplacement.WriteAsync(path, text, cancellationToken);
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
