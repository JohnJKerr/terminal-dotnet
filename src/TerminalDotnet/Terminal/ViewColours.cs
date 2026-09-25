using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace TerminalDotnet.Terminal;

/// <summary>
/// Colours a view the toolkit would otherwise draw in its theme. Each colour
/// is asked for as the view is drawn, so a view that follows some state, such
/// as a run's outcome, changes colour when the state does.
/// </summary>
internal static class ViewColours
{
    /// <summary>The text in <paramref name="foreground"/>, on the ground the
    /// view's theme gives it.</summary>
    public static void ColourText(View view, Func<Color> foreground) =>
        Colour(view, (_, background) => new Attribute(foreground(), background ?? Color.Black));

    /// <summary>The text as the view's theme draws it, on
    /// <paramref name="background"/>.</summary>
    public static void ColourGround(View view, Func<Color> background) =>
        Colour(view, (foreground, _) => new Attribute(foreground ?? Color.White, background()));

    public static void Colour(View view, Func<Color> foreground, Color background) =>
        Colour(view, (_, _) => new Attribute(foreground(), background));

    private static void Colour(View view, Func<Color?, Color?, Attribute> attribute) =>
        view.GettingAttributeForRole += (_, args) =>
        {
            args.Result = attribute(args.Result?.Foreground, args.Result?.Background);
            args.Handled = true;
        };
}
