using Terminal.Gui.App;
using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace TerminalDotnet.Terminal;

/// <summary>
/// Asks where something should be written. It comes back with the path that
/// was named, or with nothing at all when the reader backed out of it.
/// </summary>
internal static class SavePrompt
{
    private const int PromptHeight = 5;

    public static string? Ask(IApplication application, string title, string suggestedPath)
    {
        string? named = null;
        using var prompt = new Window
        {
            Title = $"{title} — Enter save  Esc cancel",
            X = Pos.Center(),
            Y = Pos.Center(),
            Width = Dim.Percent(80),
            Height = PromptHeight,
            ShadowStyle = ShadowStyles.None
        };
        var path = new TextField
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = 1,
            Text = suggestedPath
        };
        var save = new Button { Text = "Save", X = 0, Y = Pos.AnchorEnd(1) };
        var cancel = new Button { Text = "Cancel", X = Pos.Right(save) + 1, Y = Pos.AnchorEnd(1) };
        save.Accepting += (_, args) =>
        {
            named = path.Text;
            args.Handled = true;
            application.RequestStop(prompt);
        };
        cancel.Accepting += (_, args) =>
        {
            args.Handled = true;
            application.RequestStop(prompt);
        };
        path.KeyDown += (_, key) =>
        {
            if (key.NoShift.KeyCode != KeyCode.Enter)
            {
                return;
            }

            named = path.Text;
            key.Handled = true;
            application.RequestStop(prompt);
        };
        prompt.Add(path, save, cancel);
        path.SetFocus();
        application.Run(prompt);
        return string.IsNullOrWhiteSpace(named) ? null : named;
    }
}
