using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;

namespace TerminalDotnet.Terminal;

/// <summary>The framed view that scrolls whichever preview text is showing.</summary>
public sealed class PreviewScrollView : View
{
    public PreviewScrollView(Func<View> shown)
    {
        MouseEvent += (_, mouse) =>
        {
            if (mouse.Flags.HasFlag(MouseFlags.WheeledDown))
            {
                shown().ScrollVertical(1);
                mouse.Handled = true;
            }
            else if (mouse.Flags.HasFlag(MouseFlags.WheeledUp))
            {
                shown().ScrollVertical(-1);
                mouse.Handled = true;
            }
        };
    }
}
