using TerminalDotnet.Changes;
using TerminalDotnet.Explorer;
using TerminalDotnet.Files;
using TerminalDotnet.Testing;

namespace TerminalDotnet.Terminal;

public static class PanelShortcuts
{
    public static IReadOnlyList<string> For(
        PanelKind panel,
        FileExplorerState fileState,
        ChangesetState changesetState,
        ExplorerState testState,
        bool searchFocused = false) => searchFocused
        ? SearchingShortcuts
        :
        [
            "Tab pane",
            "s search",
            .. PanelShortcutsFor(panel, fileState, changesetState, testState),
            "^K commands",
            "q quit"
        ];

    /// <summary>Every letter types into the search box, so the line offers only
    /// the two ways out of it and the one command that still answers.</summary>
    private static readonly IReadOnlyList<string> SearchingShortcuts =
        ["Enter keep search", "Esc clear search", "^K commands"];

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

        var navigation = Navigation();
        IReadOnlyList<string> selection = state.VisibleNodes[state.SelectedIndex].Kind == FileNodeKind.File
            ? [.. navigation, "Enter/e edit", "p preview"]
            : [.. navigation, "Space/Enter fold"];
        return [.. selection, .. FoldAllShortcut(FileGroupExpansion(state.VisibleNodes))];
    }

    private static IReadOnlyList<string> ChangesetShortcuts(ChangesetState state)
    {
        if (state.Files.Count == 0)
        {
            return [];
        }

        IReadOnlyList<string> navigation = [.. Navigation(), "Enter/d diff"];
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

        var shortcuts = new List<string>();
        if (state.VisibleNodes[state.SelectedIndex].Kind != TestNodeKind.Test)
        {
            shortcuts.Add("Space fold");
        }

        shortcuts.AddRange(FoldAllShortcut(TestGroupExpansion(state.VisibleNodes)));
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
            ? ["o output", "l rerun", "u failures", "f next failure"]
            : ["o output", "l rerun"];
    }

    private static IEnumerable<bool> FileGroupExpansion(IReadOnlyList<VisibleFileNode> nodes) => nodes
        .Where(node => node.Kind != FileNodeKind.File)
        .Select(node => node.IsExpanded);

    private static IEnumerable<bool> TestGroupExpansion(IReadOnlyList<VisibleTestNode> nodes) => nodes
        .Where(node => node.Kind != TestNodeKind.Test)
        .Select(node => node.IsExpanded);

    private static IReadOnlyList<string> FoldAllShortcut(IEnumerable<bool> groupExpansion)
    {
        var expansion = groupExpansion.ToArray();
        if (expansion.Length == 0)
        {
            return [];
        }

        return expansion.Any(isExpanded => isExpanded) ? ["z fold all"] : ["z unfold all"];
    }

    private static IReadOnlyList<string> Navigation() => ["↑/k up", "↓/j down"];

    private static bool IsRunning(ExplorerState state) => state.Status == ExplorerStatus.Running;

    private static bool IsFailed(TestResult result) => result.Outcome == TestOutcome.Failed;
}
