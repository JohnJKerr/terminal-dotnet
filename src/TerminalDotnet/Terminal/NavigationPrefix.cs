namespace TerminalDotnet.Terminal;

/// <summary>
/// The "g" prefix, which locks navigation on for exactly as long as g is held
/// down. One panel after another can be named without reaching for g again,
/// and letting go unlocks it, so the keyboard is never left navigating.
///
/// A terminal only reports a key being released under the kitty keyboard
/// protocol. Where that is not negotiated there is no way to know g is still
/// down, so the lock is never offered rather than left with nothing to close
/// it.
/// </summary>
public sealed class NavigationPrefix
{
    public bool IsWaiting { get; private set; }

    public void Pressed(bool keyReleasesReported) => IsWaiting = keyReleasesReported;

    public void Released() => IsWaiting = false;

    public void Stop() => IsWaiting = false;
}
