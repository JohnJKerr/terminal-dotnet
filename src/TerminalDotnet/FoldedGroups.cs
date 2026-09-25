namespace TerminalDotnet;

/// <summary>
/// The groups of a tree the reader has folded, such as a project, a folder or
/// a test class. Folding everything folds every group unless every group is
/// already folded, in which case it unfolds them all.
/// </summary>
internal sealed class FoldedGroups
{
    private readonly HashSet<string> folded = [];

    public bool IsExpanded(string key) => !folded.Contains(key);

    public void Toggle(string key)
    {
        if (!folded.Add(key))
        {
            folded.Remove(key);
        }
    }

    public void ToggleAll(IReadOnlyList<string> groupKeys)
    {
        var foldAll = groupKeys.Any(IsExpanded);
        folded.Clear();
        if (foldAll)
        {
            folded.UnionWith(groupKeys);
        }
    }
}
