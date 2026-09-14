namespace TerminalDotnet.Files;

/// <summary>
/// Saving one file is rarely one edit: an editor writes a scratch copy and
/// renames it over the original, and an agent working through a change touches
/// a dozen files in as many seconds. Reloading per edit would leave git and the
/// file listings running continuously, so edits are held until the tree has been
/// quiet and the whole burst becomes a single reload.
/// </summary>
public sealed class EditBurst(TimeSpan quietDuration)
{
    public static readonly TimeSpan DefaultQuietDuration = TimeSpan.FromMilliseconds(400);

    /// <summary>The watcher reports from a thread of its own while the terminal
    /// asks from its own loop, so the burst is held under a lock.</summary>
    private readonly Lock gate = new();
    private TimeSpan? lastEdit;

    public EditBurst() : this(DefaultQuietDuration)
    {
    }

    public void Noticed(string path, TimeSpan at)
    {
        if (!WorkspaceEdits.Matters(path))
        {
            return;
        }

        Noticed(at);
    }

    public void Noticed(TimeSpan at)
    {
        lock (gate)
        {
            lastEdit = at;
        }
    }

    /// <summary>Drops the burst without reloading, for when the panels have
    /// just been brought up to date for some other reason.</summary>
    public void Forget()
    {
        lock (gate)
        {
            lastEdit = null;
        }
    }

    /// <summary>Reports a settled burst once and takes it, so the caller can
    /// ask on every frame without reloading the panels over and over.</summary>
    public bool SettledAt(TimeSpan now)
    {
        lock (gate)
        {
            if (lastEdit is not { } edit || now - edit < quietDuration)
            {
                return false;
            }

            lastEdit = null;
            return true;
        }
    }
}
