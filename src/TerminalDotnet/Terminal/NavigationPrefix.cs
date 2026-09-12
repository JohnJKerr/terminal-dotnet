namespace TerminalDotnet.Terminal;

/// <summary>
/// The "g" prefix, and how long it waits for somewhere to go.
///
/// Holding g down and naming one panel after another is the gesture worth
/// serving, but a terminal only reports a key being released under the kitty
/// keyboard protocol. Where that is negotiated, letting g up after arriving
/// somewhere ends the wait. Where it is not, no release is ever reported and
/// the wait simply stays open. Either way a key that names nowhere ends it,
/// so a tap of g still arms the prefix for the key that follows.
/// </summary>
public sealed class NavigationPrefix
{
    private bool reachedSomewhere;

    public bool IsWaiting { get; private set; }

    public void Arm() => IsWaiting = true;

    public void Reached()
    {
        IsWaiting = true;
        reachedSomewhere = true;
    }

    public void Released()
    {
        if (reachedSomewhere)
        {
            Stop();
        }
    }

    public void Stop()
    {
        IsWaiting = false;
        reachedSomewhere = false;
    }
}
