using Terminal.Gui.Drawing;

namespace TerminalDotnet.Terminal;

public enum DiffLineTone
{
    Context,
    Added,
    Removed,
    Hunk,
    Meta
}

public sealed record DiffLine(string Text, DiffLineTone Tone);

public static class DiffAppearance
{
    private static readonly string[] MetaPrefixes =
        ["+++", "---", "diff ", "index ", "new file", "deleted file", "similarity ", "rename "];

    public static IReadOnlyList<DiffLine> LinesFrom(string diff) => diff.Length == 0
        ? []
        : diff.ReplaceLineEndings("\n")
            .Split('\n')
            .Select(line => new DiffLine(line, ToneFor(line)))
            .ToArray();

    public static Color ForegroundFor(DiffLineTone tone) => tone switch
    {
        DiffLineTone.Added => Color.BrightGreen,
        DiffLineTone.Removed => Color.BrightRed,
        DiffLineTone.Hunk => Color.BrightCyan,
        DiffLineTone.Meta => Color.DarkGray,
        _ => Color.White
    };

    private static DiffLineTone ToneFor(string line)
    {
        if (MetaPrefixes.Any(prefix => line.StartsWith(prefix, StringComparison.Ordinal)))
        {
            return DiffLineTone.Meta;
        }

        if (line.StartsWith("@@", StringComparison.Ordinal))
        {
            return DiffLineTone.Hunk;
        }

        if (line.StartsWith('+'))
        {
            return DiffLineTone.Added;
        }

        return line.StartsWith('-') ? DiffLineTone.Removed : DiffLineTone.Context;
    }
}
