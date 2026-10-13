using TerminalDotnet.Explorer;
using TerminalDotnet.Testing;

namespace TerminalDotnet.Terminal;

/// <summary>Names every test under the selected row, grouped by project so
/// tests with the same name in different projects stay distinct.</summary>
public static class TestSelectionText
{
    public static string From(ExplorerState state) => state.VisibleNodes.Count == 0
        ? ""
        : string.Join("\n", state.VisibleNodes[state.SelectedIndex].Tests
            .GroupBy(test => test.ProjectPath)
            .OrderBy(project => project.Key, StringComparer.Ordinal)
            .SelectMany(ProjectLines));

    private static IEnumerable<string> ProjectLines(IGrouping<string, TestCase> project) =>
        [project.Key, .. project
            .OrderBy(test => test.FullyQualifiedName, StringComparer.Ordinal)
            .Select(test => $"  {test.FullyQualifiedName}")];
}
