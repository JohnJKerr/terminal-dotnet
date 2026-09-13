namespace TerminalDotnet.Terminal;

public sealed record PreviewSourceHighlight(string Text, int Row, bool Visible)
{
    public static PreviewSourceHighlight From(
        string source,
        int sourceLine,
        int viewportTop,
        int viewportHeight,
        bool enabled)
    {
        var lines = source.Replace("\r\n", "\n").Split('\n');
        var index = sourceLine - 1;
        var row = index - viewportTop;
        var visible = enabled && index >= 0 && index < lines.Length && row >= 0 && row < viewportHeight;
        return new PreviewSourceHighlight(
            index >= 0 && index < lines.Length ? lines[index] : "",
            row,
            visible);
    }
}
