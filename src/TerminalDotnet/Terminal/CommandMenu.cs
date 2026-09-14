namespace TerminalDotnet.Terminal;

public sealed record CommandMenuEntry(string Keys, string Description);

public sealed record CommandMenuSection(string Title, IReadOnlyList<CommandMenuEntry> Entries);

public sealed record CommandMenuRow(string Text, bool IsHeading);

/// <summary>
/// Every command the shell and its panels answer to. The status line only has
/// room for the commands that apply to the selection in front of the reader,
/// so this is the whole catalogue, shown on its own.
/// </summary>
public static class CommandMenu
{
    private const string Indent = "  ";
    private const string Gap = "  ";

    public static IReadOnlyList<CommandMenuSection> Sections() =>
    [
        new("Anywhere", Anywhere),
        new("Explorer", FileTree),
        new("Files", FileTree),
        new("Tests", Tests),
        new("Issues", Issues),
        new("Changes", Changes),
        new("Comments", Comments),
        new("Flags", Flags),
        new("Preview", Preview),
        new("Diff", Diff)
    ];

    public static IReadOnlyList<CommandMenuRow> Rows()
    {
        var sections = Sections();
        var keyColumn = sections
            .SelectMany(section => section.Entries)
            .Max(entry => entry.Keys.Length);
        return [.. sections.SelectMany((section, index) => RowsFor(section, keyColumn, index))];
    }

    private static IEnumerable<CommandMenuRow> RowsFor(
        CommandMenuSection section,
        int keyColumn,
        int index) =>
    [
        .. index == 0 ? Array.Empty<CommandMenuRow>() : [new CommandMenuRow("", false)],
        new CommandMenuRow(section.Title, true),
        .. section.Entries.Select(entry => RowFor(entry, keyColumn))
    ];

    private static CommandMenuRow RowFor(CommandMenuEntry entry, int keyColumn) => new(
        $"{Indent}{entry.Keys.PadRight(keyColumn)}{Gap}{entry.Description}",
        false);

    private static readonly IReadOnlyList<CommandMenuEntry> Anywhere =
    [
        new("E", "go to the Explorer"),
        new("F", "go to the Files"),
        new("T", "go to the Tests"),
        new("I", "go to the Issues"),
        new("G", "go to the Changes"),
        new("C", "go to the Comments"),
        new("L", "go to the Flags"),
        new("Tab", "move between search, panels and rows"),
        new("←", "focus the panel list"),
        new("→", "focus the rows"),
        new("s", "search the active panel"),
        new("Enter", "leave the search, keeping it"),
        new("Esc", "close what is open, or clear the search"),
        new("Ctrl+K", "show this list"),
        new("q", "quit, asking first if comments would be lost")
    ];

    /// <summary>The Explorer and the Files browse the same kind of tree, so
    /// they answer to the same keys.</summary>
    private static readonly IReadOnlyList<CommandMenuEntry> FileTree =
    [
        .. Navigation,
        new("Space/Enter", "fold or unfold a folder"),
        new("z", "fold or unfold every folder"),
        new("Enter/e", "edit the file"),
        new("p", "preview the file"),
        new("1", "show only updated files")
    ];

    private static readonly IReadOnlyList<CommandMenuEntry> Tests =
    [
        .. Navigation,
        new("Space", "fold or unfold a suite"),
        new("z", "fold or unfold every suite"),
        new("Enter/r", "run the selection"),
        new("l", "rerun the last run"),
        new("u", "rerun the failures"),
        new("f", "jump to the next failure"),
        new("c", "cancel the run"),
        new("o", "show the captured output"),
        new("e", "edit the test"),
        new("p", "preview the test"),
        new("1", "show only updated tests")
    ];

    private static readonly IReadOnlyList<CommandMenuEntry> Changes =
    [
        .. Navigation,
        new("Enter/d", "show the diff"),
        new("e", "edit the file"),
        new("p", "preview the file"),
        new("r", "restore a deleted file")
    ];

    private static readonly IReadOnlyList<CommandMenuEntry> Issues =
    [
        .. Navigation,
        new("Enter/e", "edit the issue's file"),
        new("p", "preview the issue's file"),
        new("y", "copy the issue"),
        new("1", "filter errors"),
        new("2", "filter warnings")
    ];

    private static readonly IReadOnlyList<CommandMenuEntry> Comments =
    [
        .. Navigation,
        new("Enter/v", "read the comment"),
        new("e", "edit the comment"),
        new("p", "preview the file it is against"),
        new("d", "delete the comment"),
        new("y", "copy every comment to the clipboard"),
        new("w", "save every comment to a file"),
        new("x", "clear every comment")
    ];

    private static readonly IReadOnlyList<CommandMenuEntry> Flags =
    [
        .. Navigation,
        new("Enter/e", "edit the flag's file"),
        new("p", "preview the flag's file"),
        new("1/2/3/4", "filter Tasks, Review, Warning or Improve")
    ];

    private static readonly IReadOnlyList<CommandMenuEntry> Preview =
    [
        .. Navigation,
        new("PgUp/PgDn", "move a screen at a time"),
        new("Home/End", "jump to the first or last line"),
        new("n", "preview the next row of the panel"),
        new("N", "preview the previous row of the panel"),
        new("e", "edit the file"),
        new("c", "comment on the file"),
        new("Esc", "close the preview")
    ];

    private static readonly IReadOnlyList<CommandMenuEntry> Diff =
    [
        .. Navigation,
        new("n", "show the next file's diff"),
        new("N", "show the previous file's diff"),
        new("Esc", "close the diff")
    ];

    private static IReadOnlyList<CommandMenuEntry> Navigation =>
    [
        new("↑/k", "move up"),
        new("↓/j", "move down")
    ];
}
