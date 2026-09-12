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
/// protocol. Where it is not negotiated no release ever arrives, g reads as
/// held, and the wait stays open until a key names nowhere.
/// </summary>
public sealed class NavigationPrefix
{
    private bool isHeld;
    private bool reachedSomewhere;

    public bool IsWaiting { get; private set; }

    public void Arm()
    {
        IsWaiting = true;
        isHeld = true;
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
