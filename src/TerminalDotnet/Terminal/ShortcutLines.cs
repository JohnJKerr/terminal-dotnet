namespace TerminalDotnet.Terminal;

/// <summary>
/// The shortcuts packed into the rows the status line keeps for them. A break
/// falls between two shortcuts rather than through one, and the shortcuts that
/// neither row can hold are left to the command list on ?.
/// </summary>
public static class ShortcutLines
{
    public const int Rows = 2;

    private const string Gap = "  ";

    public static IReadOnlyList<string> For(IReadOnlyList<string> shortcuts, int width)
    {
        if (width <= 0)
        {
            return [string.Join(Gap, shortcuts)];
        }

        var lines = new List<string>();
        var line = string.Empty;
        foreach (var shortcut in shortcuts)
        {
            var extended = line.Length == 0 ? shortcut : $"{line}{Gap}{shortcut}";
            if (line.Length == 0 || extended.Length <= width)
            {
                line = extended;
                continue;
            }

            if (lines.Count == Rows - 1)
            {
                break;
            }

            lines.Add(line);
            line = shortcut;
        }

        lines.Add(line);
        return lines;
    }
}
