using TerminalDotnet.Changes;
using TerminalDotnet.Files;

namespace TerminalDotnet.Terminal;

public sealed class ExplorerEditorWorkflow(
    IReadOnlyList<FileExplorerSession> explorers,
    ChangesetSession changes,
    IFileOpener editor,
    string target)
{
    public async Task OpenAsync(
        string path,
        int line,
        CancellationToken cancellationToken = default)
    {
        await editor.OpenAsync(path, line, cancellationToken);
        foreach (var explorer in explorers)
        {
            await explorer.LoadAsync(target, cancellationToken);
        }

        await changes.LoadAsync(target, cancellationToken);
    }
}
