using TerminalDotnet.Files;

namespace TerminalDotnet.Terminal;

/// <summary>
/// The tests and the issues describe the build they came from, so they are
/// only worth building again once the tree has been edited since. Starting a
/// rebuild answers the edits made before it; an edit that lands while it builds
/// is kept for the next, because the build may already have read past it.
/// </summary>
public sealed class EditsSinceTheBuild
{
    /// <summary>The watcher reports from a thread of its own while the terminal
    /// asks from its own loop, so the edits are held under a lock.</summary>
    private readonly Lock gate = new();
    private bool edited;

    public void Noticed(string path)
    {
        if (!WorkspaceEdits.Matters(path))
        {
            return;
        }

        Noticed();
    }

    /// <summary>For a save whose path is not known, such as whatever the reader
    /// wrote while the editor held the screen.</summary>
    public void Noticed()
    {
        lock (gate)
        {
            edited = true;
        }
    }

    public void Built()
    {
        lock (gate)
        {
            edited = false;
        }
    }

    /// <summary>Only a move onto the tests or the issues counts: choosing the
    /// panel the reader is already on opens nothing.</summary>
    public bool WorthRebuildingOnOpening(PanelKind from, PanelKind to)
    {
        if (from == to || to is not (PanelKind.Tests or PanelKind.Issues))
        {
            return false;
        }

        lock (gate)
        {
            return edited;
        }
    }
}
