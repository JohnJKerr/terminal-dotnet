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
        new("Tests", Tests),
        new("Changes", Changes),
        new("Issues", Issues),
        new("Comments", Comments),
        new("Preview", Preview)
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
        new("0", "go to the Preview"),
        new("1", "go to the Explorer"),
        new("2", "go to the Tests"),
        new("3", "go to the Changes"),
        new("4", "go to the Issues"),
        new("5", "go to the Comments"),
        new("Tab/Shift+Tab", "go to the next or previous panel"),
        new("/", "search the active panel"),
        new("Enter", "leave the search, keeping it"),
        new("Esc", "close what is open, or clear the search"),
        new("Ctrl+R", "refresh and rebuild the workspace"),
        new("?", "show this list"),
        new("q", "quit, asking first if comments would be lost")
    ];

    private static readonly IReadOnlyList<CommandMenuEntry> FileTree =
    [
        .. Navigation,
        new("Space/Enter", "fold or unfold a folder"),
        new("z", "fold or unfold every folder"),
        new("Enter/e", "edit the file"),
        new("p", "preview the file"),
        new("A", "show every file"),
        new("U", "show only updated files")
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
        new("U", "show only updated tests"),
        new("F", "show only failing tests"),
        new("P", "show only passing tests"),
        new("L", "show only the last run"),
        new("N", "show only tests not yet run")
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
        new("X", "filter errors"),
        new("W", "filter warnings"),
        new("F", "filter flags")
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

    private static readonly IReadOnlyList<CommandMenuEntry> Preview =
    [
        .. Navigation,
        new("PgUp/PgDn", "move a screen at a time"),
        new("Home/End", "jump to the first or last line"),
        new("n", "preview the next row of the panel"),
        new("N", "preview the previous row of the panel"),
        new("e", "edit the file"),
        new("c", "comment on the file")
    ];

    private static IReadOnlyList<CommandMenuEntry> Navigation =>
    [
        new("↑/k", "move up"),
        new("↓/j", "move down")
    ];
}
