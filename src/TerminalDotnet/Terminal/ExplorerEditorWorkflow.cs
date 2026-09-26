using TerminalDotnet.Changes;
using TerminalDotnet.Files;
using TerminalDotnet.Issues;

namespace TerminalDotnet.Terminal;

/// <summary>
/// Opening the editor and refreshing what it changed are separate steps,
/// because the terminal has to be down while the editor holds the screen.
/// Refreshing before it comes back would leave the shell bare for as long as
/// the reloads take, so the terminal returns first and refreshes behind itself.
/// </summary>
public sealed class ExplorerEditorWorkflow(
    IReadOnlyList<FileExplorerSession> explorers,
    ChangesetSession changes,
    IFileOpener editor,
    string target,
    IssueSession issues)
{
    private readonly PanelReload reload = new(explorers, changes, target, issues);

    public Task OpenAsync(string path, int line, CancellationToken cancellationToken = default) =>
        editor.OpenAsync(path, line, cancellationToken);

    /// <summary>A reader who has just left the editor is waiting on the result
    /// of what they wrote, so the build behind the issues is worth its wait.
    /// </summary>
    public Task RefreshAsync(
        Func<Task> onPanelRefreshed,
        CancellationToken cancellationToken = default) =>
        reload.EverythingAsync(onPanelRefreshed, cancellationToken);
}
