using TerminalDotnet.Comments;
using TerminalDotnet.Explorer;

namespace TerminalDotnet.Terminal;

/// <summary>Copies the selected, currently filtered test subtree to the clipboard.</summary>
public sealed class TestSelectionCopier(ICommentClipboard clipboard)
{
    public async Task<bool?> CopyAsync(
        ExplorerState state,
        CancellationToken cancellationToken = default)
    {
        var text = TestSelectionText.From(state);
        return text.Length == 0
            ? null
            : await clipboard.TryCopyAsync(text, cancellationToken);
    }
}
