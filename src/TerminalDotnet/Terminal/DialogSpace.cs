namespace TerminalDotnet.Terminal;

/// <summary>
/// Whether a console has room for a message box. The toolkit sizes a box from
/// the console it is drawn on and keeps three columns for the box's own
/// border, so a narrower console leaves the text a negative width and the
/// toolkit throws before the box appears. Windows consoles report no width at
/// all in some hosts, which is where that happened.
/// </summary>
public static class DialogSpace
{
    /// <summary>The columns the toolkit keeps for a box's own border.</summary>
    private const int Border = 3;

    /// <returns>Whether a box can be drawn in a console this wide.</returns>
    public static bool Fits(int consoleWidth) => consoleWidth >= Border;

    /// <returns>Whether a box can be drawn in the console this is running in.
    /// A console that cannot say how wide it is has no room for one.</returns>
    public static bool FitsTheConsole()
    {
        try
        {
            return Fits(Console.WindowWidth);
        }
        catch (IOException)
        {
            return false;
        }
    }
}
