using TerminalDotnet.Changes;
using TerminalDotnet.Explorer;
using TerminalDotnet.Files;

namespace TerminalDotnet.Terminal;

public sealed class PanelStartup(
    FileExplorerSession files,
    ChangesetSession changes,
    TestExplorerSession tests,
    string target)
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
        .. Pending(files.State.Loading, token => files.LoadAsync(target, token)),
        .. Pending(changes.State.Loading, token => changes.LoadAsync(target, token)),
        .. Pending(
            tests.State.Status == ExplorerStatus.Loading,
            token => tests.LoadAsync(target, token))
    ];

    private static IEnumerable<Func<CancellationToken, Task>> Pending(
        bool stillLoading,
        Func<CancellationToken, Task> load) => stillLoading ? [load] : [];
}
