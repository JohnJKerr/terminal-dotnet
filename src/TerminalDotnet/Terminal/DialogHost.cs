using System.Diagnostics;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
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
    public void ShowText(IApplication application, string title, List<List<Cell>> lines)
    {
        using var dialog = FullScreen(title);
        var text = Output(dialog, followsTheRun: false);
        text.Show(lines);
        Over(() => application.Run(dialog));
    }

    /// <summary>Output that is still being reported, replaced as it changes
    /// for as long as the reader has it open.</summary>
    public void ShowFollowing(
        IApplication application,
        string title,
        List<List<Cell>> lines,
        Func<(string Title, string Output)> current)
    {
        using var dialog = FullScreen(title);
        var text = Output(dialog, followsTheRun: true);
        text.Show(lines);
        var initial = current();
        var showingTitle = initial.Title;
        var showingOutput = initial.Output;
        var shownAt = TimeSpan.Zero;
        var clock = Stopwatch.StartNew();
        var open = true;
        application.AddTimeout(RunningOutputRefresh.Interval, () =>
        {
            if (!open)
            {
                return false;
            }

            var latest = current();
            if (!RunningOutputRefresh.Replace(
                showingTitle,
                showingOutput,
                latest.Title,
                latest.Output,
                clock.Elapsed - shownAt))
            {
                return true;
            }

            showingTitle = latest.Title;
            showingOutput = latest.Output;
            shownAt = clock.Elapsed;
            dialog.Title = latest.Title;
            text.Show(AnsiTestOutput.ToCells(latest.Output));
            return true;
        });
        Over(() => application.Run(dialog));
        open = false;
    }

    private static Window FullScreen(string title)
    {
        var dialog = new Window
        {
            Title = title,
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ShadowStyle = ShadowStyles.None
        };
        ViewColours.ColourGround(dialog, () => Color.Black);
        return dialog;
    }

    private static OutputView Output(Window dialog, bool followsTheRun)
    {
        var text = new OutputView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            CanFocus = true,
            FollowsTheRun = followsTheRun
        };
        ViewColours.ColourGround(text, () => Color.Black);
        dialog.Add(text);
        AddSearch(dialog, text);
        return text;
    }

    /// <summary>A / opens a prompt beneath the output that searches it as
    /// the reader types. Enter goes back to reading with the matches still
    /// marked, and Esc clears the search rather than closing the output.
    /// </summary>
    private static void AddSearch(Window dialog, OutputView text)
    {
        var prompt = new TextField { X = 0, Y = Pos.AnchorEnd(1), Width = Dim.Fill(), Visible = false };
        dialog.Add(prompt);
        text.KeyDown += (_, key) =>
        {
            if (key.AsRune.Value != '/')
            {
                return;
            }

            prompt.Text = "";
            ShowPrompt(text, prompt, shown: true);
            key.Handled = true;
        };
        prompt.TextChanged += (_, _) => text.Search(prompt.Text);
        prompt.KeyDown += (_, key) =>
        {
            if (key == Key.Esc)
            {
                text.Search("");
            }
            else if (key != Key.Enter)
            {
                return;
            }

            ShowPrompt(text, prompt, shown: false);
            key.Handled = true;
        };
    }

    private static void ShowPrompt(OutputView text, TextField prompt, bool shown)
    {
        prompt.Visible = shown;
        text.Height = Dim.Fill(shown ? 1 : 0);
        (shown ? (View)prompt : text).SetFocus();
    }
}
