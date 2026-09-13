using System.Drawing;
using Terminal.Gui.App;
using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace TerminalDotnet.Terminal;

/// <summary>
/// The box a comment is written in. It comes back with the text that was
/// written, or with nothing at all when the reader backed out of it.
/// </summary>
internal static class CommentDialog
{
    public static string? Ask(IApplication application, string displayPath, string existing)
    {
        string? written = null;
        using var dialog = new Window
        {
            Title = $"Comment — {displayPath} — Ctrl+S save  Esc cancel",
            X = Pos.Center(),
            Y = Pos.Center(),
            Width = Dim.Percent(80),
            Height = Dim.Percent(60),
            ShadowStyle = ShadowStyles.None
        };
#pragma warning disable CS0618 // Terminal.Gui.Editor is a separate, non-source-compatible dependency.
        var text = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(1),
            Multiline = true,
            WordWrap = true,
            Text = existing
        };
#pragma warning restore CS0618
        var save = new Button { Text = "Save", X = 0, Y = Pos.AnchorEnd(1), IsDefault = false };
        var cancel = new Button { Text = "Cancel", X = Pos.Right(save) + 1, Y = Pos.AnchorEnd(1) };
        save.Accepting += (_, args) =>
        {
            written = text.Text;
            args.Handled = true;
            application.RequestStop(dialog);
        };
        cancel.Accepting += (_, args) =>
        {
            args.Handled = true;
            application.RequestStop(dialog);
        };
        text.KeyDown += (_, key) =>
        {
            if (!IsCtrl(key, KeyCode.S))
            {
                return;
            }

            written = text.Text;
            key.Handled = true;
            application.RequestStop(dialog);
        };
        text.Initialized += (_, _) => text.InsertionPoint = EndOf(existing);
        dialog.Add(text, save, cancel);
        text.SetFocus();
        application.Run(dialog);
        return written;
    }

    /// <summary>A note is carried on from where it left off, so reopening one
    /// puts the cursor after what is already written rather than in front of
    /// it.</summary>
    private static Point EndOf(string text)
    {
        var lines = text.Split('\n');
        return new Point(lines[^1].Length, lines.Length - 1);
    }

    private static bool IsCtrl(Key key, KeyCode keyCode) =>
        key.IsCtrl && key.NoShift.NoCtrl.NoAlt.KeyCode == keyCode;
}
