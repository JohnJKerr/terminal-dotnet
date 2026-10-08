using System.Globalization;

namespace TerminalDotnet.Terminal;

/// <summary>
/// A selection is copied as the drag ends, with no key pressed to ask for it,
/// so a toast says that it went to the clipboard and how much of it did.
/// </summary>
public static class SelectionToast
{
    public static Toast Copied(string text) => new(
        $"Copied {CountedNoun.Of(new StringInfo(text).LengthInTextElements, "character")}",
        ToastTone.Succeeded);

    public static Toast NotCopied() => new("Could not copy the selection", ToastTone.Failed);
}
