using TerminalDotnet.Changes;
using TerminalDotnet.Explorer;
using TerminalDotnet.Files;
using TerminalDotnet.Testing;

namespace TerminalDotnet.Terminal;

public static class PanelShortcuts
{
    public static string For(
        PanelKind panel,
        FileExplorerState fileState,
        ChangesetState changesetState,
        ExplorerState testState) => string.Join(
        "  ",
        ["Tab pane", "s search", .. PanelShortcutsFor(panel, fileState, changesetState, testState), "q quit"]);

    private static IReadOnlyList<string> PanelShortcutsFor(
        PanelKind panel,
        FileExplorerState fileState,
        ChangesetState changesetState,
        ExplorerState testState) => panel switch
    {
        PanelKind.Explorer => ExplorerShortcuts(fileState),
        PanelKind.Changes => ChangesetShortcuts(changesetState),
        _ => TestShortcuts(testState)
    };

    private static IReadOnlyList<string> ExplorerShortcuts(FileExplorerState state)
    {
        if (state.VisibleNodes.Count == 0)
        {
            return [];
        }

        var navigation = Navigation(state.SearchQuery);
        return state.VisibleNodes[state.SelectedIndex].Kind == FileNodeKind.File
            ? [.. navigation, "Enter/e edit", "p preview"]
            : [.. navigation, "Space/Enter fold"];
    }

    private static IReadOnlyList<string> ChangesetShortcuts(ChangesetState state)
    {
        if (state.Files.Count == 0)
        {
            return [];
        }

        IReadOnlyList<string> navigation = [.. Navigation(state.SearchQuery), "Enter/d diff"];
        return state.Files[state.SelectedIndex].Kind == ChangeKind.Deleted
            ? [.. navigation, "r restore"]
            : [.. navigation, "e edit", "p preview"];
    }

    private static IReadOnlyList<string> TestShortcuts(ExplorerState state) =>
        [.. SelectionShortcuts(state), .. RunShortcuts(state)];

    private static IReadOnlyList<string> SelectionShortcuts(ExplorerState state)
    {
        if (state.VisibleNodes.Count == 0)
        {
            return [];
        }

        var shortcuts = new List<string>(Navigation(state.SearchQuery));
        if (state.VisibleNodes[state.SelectedIndex].Kind != TestNodeKind.Test)
        {
            shortcuts.Add("Space fold");
        }

        if (!IsRunning(state))
        {
            shortcuts.Add("Enter run");
        }

        shortcuts.Add("e edit");
        shortcuts.Add("p preview");
        return shortcuts;
    }

    private static IReadOnlyList<string> RunShortcuts(ExplorerState state)
    {
        if (IsRunning(state))
        {
            return state.LastRun is null ? ["c cancel"] : ["o output", "c cancel"];
        }

        if (state.LastRun is null)
        {
            return [];
        }

        return state.LastRun.Results.Any(IsFailed)
            ? ["o output", "R rerun", "F failures"]
            : ["o output", "R rerun"];
    }

    private static IReadOnlyList<string> Navigation(string searchQuery) => searchQuery.Length == 0
        ? ["↑/k up", "↓/j down"]
        : ["↑/k up", "↓/j down", "n/N match"];

    private static bool IsRunning(ExplorerState state) => state.Status == ExplorerStatus.Running;

    private static bool IsFailed(TestResult result) => result.Outcome == TestOutcome.Failed;
}
