using TerminalDotnet.Explorer;
using TerminalDotnet.Files;
using TerminalDotnet.Testing;

namespace TerminalDotnet.Terminal;

public static class PanelShortcuts
{
    public static string For(
        PanelKind panel,
        FileExplorerState fileState,
        ExplorerState testState)
    {
        var shortcuts = new List<string> { "Tab pane", "s search" };

        if (panel == PanelKind.Explorer)
        {
            AddExplorerShortcuts(shortcuts, fileState);
        }
        else
        {
            AddTestShortcuts(shortcuts, testState);
        }

        shortcuts.Add("q quit");
        return string.Join("  ", shortcuts);
    }

    private static void AddExplorerShortcuts(
        ICollection<string> shortcuts,
        FileExplorerState state)
    {
        if (state.VisibleNodes.Count == 0)
        {
            return;
        }

        shortcuts.Add("↑/k up");
        shortcuts.Add("↓/j down");
        if (state.SearchQuery.Length > 0)
        {
            shortcuts.Add("n/N match");
        }

        if (state.VisibleNodes[state.SelectedIndex].Kind == FileNodeKind.File)
        {
            shortcuts.Add("Enter/e edit");
            shortcuts.Add("p preview");
            return;
        }

        shortcuts.Add("Space/Enter fold");
    }

    private static void AddTestShortcuts(
        ICollection<string> shortcuts,
        ExplorerState state)
    {
        if (state.VisibleNodes.Count > 0)
        {
            shortcuts.Add("↑/k up");
            shortcuts.Add("↓/j down");
            if (state.SearchQuery.Length > 0)
            {
                shortcuts.Add("n/N match");
            }

            if (state.VisibleNodes[state.SelectedIndex].Kind != TestNodeKind.Test)
            {
                shortcuts.Add("Space fold");
            }

            if (state.Status != ExplorerStatus.Running)
            {
                shortcuts.Add("Enter run");
            }
            shortcuts.Add("e edit");
            shortcuts.Add("p preview");
        }

        if (state.LastRun is not null)
        {
            shortcuts.Add("o output");
            if (state.Status != ExplorerStatus.Running)
            {
                shortcuts.Add("R rerun");
            }
        }

        if (state.Status == ExplorerStatus.Running)
        {
            shortcuts.Add("c cancel");
            return;
        }

        if (state.LastRun?.Results.Any(IsFailed) == true)
        {
            shortcuts.Add("F failures");
        }
    }

    private static bool IsFailed(TestResult result) => result.Outcome == TestOutcome.Failed;
}
