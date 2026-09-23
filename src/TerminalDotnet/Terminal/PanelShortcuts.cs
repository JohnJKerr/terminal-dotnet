using TerminalDotnet.Changes;
using TerminalDotnet.Comments;
using TerminalDotnet.Explorer;
using TerminalDotnet.Files;
using TerminalDotnet.Issues;

namespace TerminalDotnet.Terminal;

public static class PanelShortcuts
{
    public static IReadOnlyList<string> For(
        PanelKind panel,
        FileExplorerState fileState,
        ChangesetState changesetState,
        ExplorerState testState,
        CommentsState commentState,
        bool searchFocused = false,
        IssueState? issueState = null) => searchFocused
        ? SearchingShortcuts
        :
        [
            "Tab pane",
            .. panel == PanelKind.Preview ? Array.Empty<string>() : ["s search"],
            .. PanelShortcutsFor(panel, fileState, changesetState, testState, commentState, issueState),
            "^R refresh",
            "? commands",
            "q quit"
        ];

    /// <summary>The preview has nothing to search; it scrolls what it shows
    /// and steps the list it follows.</summary>
    private static readonly IReadOnlyList<string> PreviewShortcuts =
        [.. Navigation(), "PgUp/PgDn page", "n/N next/previous row", "e edit", "c comment"];

    /// <summary>Every letter types into the search box, so the line offers only
    /// the two ways out of it and the commands that still answer.</summary>
    private static readonly IReadOnlyList<string> SearchingShortcuts =
        ["Enter keep search", "Esc clear search", "^R refresh", "? commands"];

    private static IReadOnlyList<string> PanelShortcutsFor(
        PanelKind panel,
        FileExplorerState fileState,
        ChangesetState changesetState,
        ExplorerState testState,
        CommentsState commentState,
        IssueState? issueState) => panel switch
    {
        PanelKind.Explorer => ExplorerShortcuts(fileState),
        PanelKind.Changes => ChangesetShortcuts(changesetState),
        PanelKind.Issues => issueState is { Issues.Count: > 0 }
            ? [.. Navigation(), "Enter/e edit", "p preview", "y copy", "X errors", "W warnings", "F flags"]
            : ["X errors", "W warnings", "F flags"],
        PanelKind.Comments => CommentShortcuts(commentState),
        PanelKind.Preview => PreviewShortcuts,
        _ => TestShortcuts(testState)
    };

    private static IReadOnlyList<string> CommentShortcuts(CommentsState state) =>
        state.Comments.Count == 0
            ? []
            : [.. Navigation(), "Enter/v view", "e edit", "p preview", "d delete", "y copy", "w save", "x clear all"];

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
        return [.. selection, .. FoldAllShortcut(state.HasGroups, state.HasExpandedGroups)];
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

        shortcuts.AddRange(FoldAllShortcut(state.HasGroups, state.HasExpandedGroups));
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

        return state.LastRun.Summary.Failed > 0
            ? ["o output", "l rerun", "u failures", "f next failure"]
            : ["o output", "l rerun"];
    }

    private static IReadOnlyList<string> FoldAllShortcut(bool hasGroups, bool hasExpandedGroups)
    {
        if (!hasGroups)
        {
            return [];
        }

        return hasExpandedGroups ? ["z fold all"] : ["z unfold all"];
    }

    private static IReadOnlyList<string> Navigation() => ["↑/k up", "↓/j down"];

    private static bool IsRunning(ExplorerState state) => state.Status == ExplorerStatus.Running;

}
