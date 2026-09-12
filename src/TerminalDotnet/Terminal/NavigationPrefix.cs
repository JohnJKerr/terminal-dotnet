namespace TerminalDotnet.Terminal;

/// <summary>
/// The "g" prefix, and how long it waits for somewhere to go.
///
/// Held down, it keeps waiting, so one panel after another can be named
/// without reaching for g again, and letting go ends it. Tapped, it waits
/// only for the key that follows and is spent on it, so a tap never leaves
/// the keyboard locked into navigating.
///
/// A terminal only reports a key being released under the kitty keyboard
/// protocol. Holding is only offered where that is negotiated, because a wait
/// that no release can close would lock the keyboard into navigating; where it
/// is not, every g is spent like a tap.
/// </summary>
public sealed class NavigationPrefix
{
    private bool isHeld;
    private bool reachedSomewhere;

    public bool IsWaiting { get; private set; }

    public void Arm(bool keyReleasesReported)
    {
        IsWaiting = true;
        isHeld = keyReleasesReported;
    }

    public void Reached()
    {
        if (!isHeld)
        {
            Stop();
            return;
        }

        IsWaiting = true;
        reachedSomewhere = true;
    }

    public void Released()
    {
        isHeld = false;
        if (reachedSomewhere)
        {
            Stop();
        }
    }

    public void Stop()
    {
        IsWaiting = false;
        isHeld = false;
        reachedSomewhere = false;
    }
}
