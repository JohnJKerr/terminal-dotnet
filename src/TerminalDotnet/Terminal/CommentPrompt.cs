namespace TerminalDotnet.Terminal;

/// <summary>What the reader is asked before the comments are thrown away.
/// </summary>
public static class CommentPrompt
{
    public static string QuitLoses(int count) => count == 1
        ? "1 comment has not been copied or saved. Quitting loses it."
        : $"{count} comments have not been copied or saved. Quitting loses them.";

    public static string ClearAll(int count) => count == 1
        ? "Clear 1 comment? This cannot be undone."
        : $"Clear all {count} comments? This cannot be undone.";
}
