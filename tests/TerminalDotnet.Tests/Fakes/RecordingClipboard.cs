using TerminalDotnet.Comments;

namespace TerminalDotnet.Tests.Fakes;

/// <summary>A clipboard that keeps the last text it took, or refuses it.</summary>
internal sealed class RecordingClipboard : ICommentClipboard
{
    public bool CopySucceeds { get; init; } = true;

    public string? Copied { get; private set; }

    public Task<bool> TryCopyAsync(string text, CancellationToken cancellationToken = default)
    {
        if (!CopySucceeds)
        {
            return Task.FromResult(false);
        }

        Copied = text;
        return Task.FromResult(true);
    }
}
