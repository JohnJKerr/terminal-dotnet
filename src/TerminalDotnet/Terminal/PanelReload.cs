using TerminalDotnet.Changes;
using TerminalDotnet.Files;
using TerminalDotnet.Issues;

namespace TerminalDotnet.Terminal;

/// <summary>
/// Bringing the panels back in line with what is on disk now. The issues are
/// kept apart from the rest because they cannot answer without a build, which
/// takes longer than every other panel put together: a reload that runs while
/// the reader is still typing can afford the file listings and git, but not
/// that.
/// </summary>
public sealed class PanelReload(
    IReadOnlyList<FileExplorerSession> explorers,
    ChangesetSession changes,
    string target,
    IssueSession issues)
{
    private delegate Task PanelLoad(CancellationToken cancellationToken);

    /// <summary>Every panel that answers by reading the working tree.</summary>
    public Task FromDiskAsync(
        Func<Task> onPanelReloaded,
        CancellationToken cancellationToken = default) =>
        ReloadAsync(ReadingPanels(), onPanelReloaded, cancellationToken);

    public Task EverythingAsync(
        Func<Task> onPanelReloaded,
        CancellationToken cancellationToken = default) =>
        ReloadAsync([.. ReadingPanels(), .. BuildingPanels()], onPanelReloaded, cancellationToken);

    /// <summary>Reports each panel as it lands, because the build behind the
    /// issues takes far longer than the rest and must not hold them back.
    /// </summary>
    private async Task ReloadAsync(
        IEnumerable<PanelLoad> loads,
        Func<Task> onPanelReloaded,
        CancellationToken cancellationToken)
    {
        foreach (var load in loads)
        {
            await load(cancellationToken);
            await onPanelReloaded();
        }
    }

    private IEnumerable<PanelLoad> ReadingPanels() =>
    [
        .. explorers.Select<FileExplorerSession, PanelLoad>(
            explorer => token => explorer.LoadAsync(target, token)),
        token => changes.LoadAsync(target, token),
        token => issues.LoadFlagsAsync(target, token)
    ];

    private IEnumerable<PanelLoad> BuildingPanels() => [token => issues.LoadAsync(target, token)];
}
