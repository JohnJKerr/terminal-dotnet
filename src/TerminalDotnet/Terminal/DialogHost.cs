using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace TerminalDotnet.Terminal;

/// <summary>
/// Runs dialogs over the panels. The shell listens for keys across the whole
/// application, so the panels stand down for as long as something is open in
/// front of them; otherwise a q typed into a comment would quit rather than be
/// written. Dialogs open over one another — a comment is written over the
/// preview it was prompted by — so what is open is counted rather than
/// flagged.
/// </summary>
internal sealed class DialogHost
{
    private int open;

    public bool AnyOpen => open > 0;

    public T Over<T>(Func<T> show)
    {
        open++;
        try
        {
            return show();
        }
        finally
        {
            open--;
        }
    }

    public void Over(Action show) => Over<object?>(() =>
    {
        show();
        return null;
    });

    /// <summary>Coloured text over the whole screen, for reading and
    /// scrolling until the reader closes it.</summary>
    public void ShowText(IApplication application, string title, List<List<Cell>> lines, bool wordWrap)
    {
        using var dialog = new Window
        {
            Title = title,
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ShadowStyle = ShadowStyles.None
        };
        var text = new ColoredTextView(wordWrap) { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
        text.Load(lines);
        ViewColours.ColourGround(dialog, () => Color.Black);
        ViewColours.ColourGround(text, () => Color.Black);
        dialog.Add(text);
        Over(() => application.Run(dialog));
    }
}
