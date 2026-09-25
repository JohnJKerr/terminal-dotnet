using Terminal.Gui.Drivers;
using Terminal.Gui.Input;

namespace TerminalDotnet.Terminal;

/// <summary>Whether a pressed key is the one a binding names. A bare letter
/// and its capital are different bindings, so Shift has to match too.</summary>
internal static class KeyMatch
{
    public static bool Is(Key key, KeyCode keyCode) =>
        !key.IsShift && key.NoShift.KeyCode == keyCode;

    public static bool IsCtrl(Key key, KeyCode keyCode) =>
        key.IsCtrl && key.NoShift.NoCtrl.NoAlt.KeyCode == keyCode;
}
