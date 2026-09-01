using TerminalDotnet.Files;

namespace TerminalDotnet.Terminal;

public sealed class ExplorerEditorWorkflow(
    FileExplorerSession explorer,
    IFileOpener editor,
    string target)
{
    public async Task OpenAsync(
        string path,
        int line,
        CancellationToken cancellationToken = default)
    {
        await editor.OpenAsync(path, line, cancellationToken);
        await explorer.LoadAsync(target, cancellationToken);
    }
}
