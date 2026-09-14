using TerminalDotnet.Changes;
using TerminalDotnet.Files;
using TerminalDotnet.Flags;
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
    FlagSession? flags = null,
    IssueSession? issues = null)
{
    private delegate Task PanelLoad(CancellationToken cancellationToken);

    public Task OpenAsync(string path, int line, CancellationToken cancellationToken = default) =>
        editor.OpenAsync(path, line, cancellationToken);

    /// <summary>Reports each panel as it lands, because the build behind the
    /// issues takes far longer than the rest and must not hold them back.
    /// </summary>
    public async Task RefreshAsync(
        Func<Task> onPanelRefreshed,
        CancellationToken cancellationToken = default)
    {
        foreach (var load in EditedPanels())
        {
            await load(cancellationToken);
            await onPanelRefreshed();
        }
    }

    private IEnumerable<PanelLoad> EditedPanels() =>
    [
        .. explorers.Select<FileExplorerSession, PanelLoad>(
            explorer => token => explorer.LoadAsync(target, token)),
        token => changes.LoadAsync(target, token),
        .. Present<FlagSession>(flags, token => flags!.LoadAsync(target, token)),
        .. Present<IssueSession>(issues, token => issues!.LoadAsync(target, token))
    ];

    private static IEnumerable<PanelLoad> Present<TSession>(TSession? panel, PanelLoad load)
        where TSession : class => panel is null ? [] : [load];
}
