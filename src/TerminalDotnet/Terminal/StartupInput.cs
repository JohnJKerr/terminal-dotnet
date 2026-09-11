namespace TerminalDotnet.Terminal;

/// <summary>
/// Terminals answer the queries a driver sends while starting up, and a reply
/// whose escape sequence the driver fails to parse arrives as ordinary
/// keystrokes. Those land on single-key bindings before a reader has touched
/// the keyboard, so keys are only taken once the panels have settled.
/// </summary>
public static class StartupInput
{
    public static bool Accepts(TimeSpan sincePanelsAppeared, TimeSpan settleDuration) =>
        sincePanelsAppeared >= settleDuration;
}
