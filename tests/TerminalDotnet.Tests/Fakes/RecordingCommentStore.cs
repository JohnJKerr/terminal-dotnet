using TerminalDotnet.Comments;

namespace TerminalDotnet.Tests.Fakes;

/// <summary>A store that keeps the last text written to it, or refuses it,
/// and reports the paths it was told already hold something.</summary>
internal sealed class RecordingCommentStore(params string[] existingPaths) : ICommentStore
{
    private readonly HashSet<string> existing = [.. existingPaths];

    public bool WriteSucceeds { get; init; } = true;

    public string? Written { get; private set; }

    public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default) =>
        Task.FromResult(existing.Contains(path));

    public Task<bool> TryWriteAsync(
        string path,
        string text,
        CancellationToken cancellationToken = default)
    {
        if (!WriteSucceeds)
        {
            return Task.FromResult(false);
        }

        Written = text;
        existing.Add(path);
        return Task.FromResult(true);
    }
}
