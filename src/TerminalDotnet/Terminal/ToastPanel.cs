using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace TerminalDotnet.Terminal;

/// <summary>
/// The box a toast is shown in, over the top right of the panels. Each toast
/// replaces the one before it; one that has been said fades on its own, while
/// one still working stays up and keeps its marker turning.
/// </summary>
internal sealed class ToastPanel
{
    private const int Padding = 4;

    private readonly int rightInset;
    private readonly Label text = new() { X = 1, Y = 0 };
    private Toast? showing;
    private int shownCount;

    public ToastPanel(int rightInset)
    {
        this.rightInset = rightInset;
        ViewColours.Colour(text, Foreground, Color.Black);
        View = new View
        {
            Y = 1,
            Height = 3,
            BorderStyle = LineStyle.Rounded,
            CanFocus = false,
            TabStop = TabBehavior.NoStop,
            Visible = false
        };
        ViewColours.ColourGround(View, () => Color.Black);
        View.Add(text);
    }

    public View View { get; }

    /// <summary>A fade scheduled for an older toast must not take down the
    /// one showing now, so each fade checks it is still the latest.</summary>
    public void Show(IApplication application, Toast toast)
    {
        var shown = ++shownCount;
        ShowText(toast);
        View.Visible = true;
        application.LayoutAndDraw(true);
        if (!toast.FadesAway)
        {
            return;
        }

        application.AddTimeout(RebuildToast.FadeAfter, () =>
        {
            if (shown == shownCount)
            {
                Hide(application);
            }

            return false;
        });
    }

    /// <summary>Redraws a working toast every frame, asking for the draw
    /// because nothing else wakes the loop while the work is out.</summary>
    public void KeepTurning(IApplication application, Func<Toast> current) =>
        application.AddTimeout(ActivityMarker.FrameDuration, () =>
        {
            if (showing is not { Tone: ToastTone.Working })
            {
                return false;
            }

            ShowText(current());
            application.LayoutAndDraw(true);
            return true;
        });

    private void Hide(IApplication application)
    {
        View.Visible = false;
        showing = null;
        application.LayoutAndDraw(true);
    }

    private void ShowText(Toast toast)
    {
        showing = toast;
        var width = toast.Text.Length + Padding;
        text.Text = toast.Text;
        text.Width = toast.Text.Length;
        View.Width = width;
        View.X = Pos.AnchorEnd(width + rightInset);
    }

    private Color Foreground() => showing?.Tone switch
    {
        ToastTone.Succeeded => Color.BrightGreen,
        ToastTone.Failed => Color.BrightRed,
        ToastTone.Waiting => Color.BrightYellow,
        _ => Color.White
    };
}
