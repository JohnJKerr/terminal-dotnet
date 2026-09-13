using TerminalDotnet.Changes;
using TerminalDotnet.Explorer;
using TerminalDotnet.Files;
using TerminalDotnet.Flags;

namespace TerminalDotnet.Terminal;

public sealed class PanelStartup(
    FileExplorerSession projectFiles,
    FileExplorerSession folderFiles,
    ChangesetSession changes,
    TestExplorerSession tests,
    string target,
    FlagSession? flags = null)
{
    public async Task LoadPendingAsync(
        Func<Task> onPanelFilled,
        CancellationToken cancellationToken = default)
    {
        foreach (var load in PendingLoads())
        {
            try
            {
                await load(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            await onPanelFilled();
        }
    }

    private IEnumerable<Func<CancellationToken, Task>> PendingLoads() =>
    [
        .. Pending(projectFiles.State.Loading, token => projectFiles.LoadAsync(target, token)),
        .. Pending(
            folderFiles.State.Loading,
            token => folderFiles.LoadAsync(target, token)),
        .. Pending(changes.State.Loading, token => changes.LoadAsync(target, token)),
        .. Pending(
            tests.State.Status == ExplorerStatus.Loading,
            token => tests.LoadAsync(target, token)),
        .. flags is null ? [] : Pending(flags.State.Loading, token => flags.LoadAsync(target, token))
    ];

    private static IEnumerable<Func<CancellationToken, Task>> Pending(
        bool stillLoading,
        Func<CancellationToken, Task> load) => stillLoading ? [load] : [];
}
