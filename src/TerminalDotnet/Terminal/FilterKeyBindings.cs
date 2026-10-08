using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using static TerminalDotnet.Terminal.KeyMatch;

namespace TerminalDotnet.Terminal;

public static class FilterKeyBindings
{
    /// <summary>How far a key steps along the panel's filters: ] on to the
    /// next, [ back to the one before.</summary>
    public static int? StepFor(Key key)
    {
        if (Is(key, (KeyCode)']'))
        {
            return 1;
        }

        return Is(key, (KeyCode)'[') ? -1 : null;
    }
}
