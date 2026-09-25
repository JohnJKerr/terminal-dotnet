using System.Text.RegularExpressions;
using Terminal.Gui.Drawing;

namespace TerminalDotnet.Terminal;

public static partial class AnsiTestOutput
{
    public static List<List<Cell>> ToCells(string output) => output
        .ReplaceLineEndings("\n")
        .Split('\n')
        .Select(line => (Raw: line, Clean: CleanControlSequences(line)))
        .Where(line => line.Raw.Length == 0 || IsTestOutput(line.Clean))
        .Select(line => StyledCells(line.Clean))
        .ToList();

    private static string CleanControlSequences(string line) =>
        OscSequence().Replace(NonStyleCsiSequence().Replace(line, ""), "");

    private static bool IsTestOutput(string line)
    {
        var plain = StyleSequence().Replace(line, "").Trim();
        if (plain.Length == 0)
        {
            return false;
        }

        return ReportsAFailure(plain) || !BuildStopwatch().IsMatch(plain);
    }

    private static bool ReportsAFailure(string line) =>
        line.Contains("fail", StringComparison.OrdinalIgnoreCase) ||
        line.Contains("error", StringComparison.OrdinalIgnoreCase);

    private static List<Cell> StyledCells(string line)
    {
        var cells = new List<Cell>();
        Color foreground = Color.White;
        var bold = false;
        var start = 0;
        foreach (Match match in StyleSequence().Matches(line))
        {
            AddCells(cells, line[start..match.Index], foreground, bold);
            ApplyStyle(match.Groups[1].Value, ref foreground, ref bold);
            start = match.Index + match.Length;
        }

        AddCells(cells, line[start..], foreground, bold);
        return cells;
    }

    private static void AddCells(List<Cell> cells, string text, Color foreground, bool bold)
    {
        var color = bold ? Bright(foreground) : foreground;
        cells.AddRange(Cell.ToCellList(text, new global::Terminal.Gui.Drawing.Attribute(color, Color.Black)));
    }

    private static void ApplyStyle(string parameters, ref Color foreground, ref bool bold)
    {
        foreach (var parameter in parameters.Split(';', StringSplitOptions.RemoveEmptyEntries).DefaultIfEmpty("0"))
        {
            if (!int.TryParse(parameter, out var code))
            {
                continue;
            }

            (foreground, bold) = code switch
            {
                0 => (Color.White, false),
                1 => (foreground, true),
                >= 30 and <= 37 or >= 90 and <= 97 => (ColorFor(code), bold),
                _ => (foreground, bold)
            };
        }
    }

    private static Color ColorFor(int code) => code switch
    {
        30 => Color.Black,
        31 => Color.Red,
        32 => Color.Green,
        33 => Color.Yellow,
        34 => Color.Blue,
        35 => Color.Magenta,
        36 => Color.Cyan,
        90 => Color.DarkGray,
        91 => Color.BrightRed,
        92 => Color.BrightGreen,
        93 => Color.BrightYellow,
        94 => Color.BrightBlue,
        95 => Color.BrightMagenta,
        96 => Color.BrightCyan,
        _ => Color.White
    };

    private static readonly IReadOnlyDictionary<Color, Color> Brightened = new Dictionary<Color, Color>
    {
        [Color.Red] = Color.BrightRed,
        [Color.Green] = Color.BrightGreen,
        [Color.Blue] = Color.BrightBlue,
        [Color.Cyan] = Color.BrightCyan,
        [Color.Magenta] = Color.BrightMagenta,
        [Color.Yellow] = Color.BrightYellow
    };

    private static Color Bright(Color color) => Brightened.GetValueOrDefault(color, color);

    [GeneratedRegex(@"\x1B\[([0-9;]*)m")]
    private static partial Regex StyleSequence();

    [GeneratedRegex(@"\x1B\[[0-9;?]*[A-LN-Zfhln]")]
    private static partial Regex NonStyleCsiSequence();

    [GeneratedRegex(@"\x1B\].*?(?:\x07|\x1B\\)")]
    private static partial Regex OscSequence();

    [GeneratedRegex(@"(?:\(\d+(?:\.\d+)?s\)(?:\s*\u2192.*)?|\sRestore|\sTesting)$")]
    private static partial Regex BuildStopwatch();
}
