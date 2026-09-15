namespace TerminalDotnet.Terminal;

/// <summary>
/// Which Terminal.Gui driver draws the app. The toolkit's own choice away from
/// Windows is the ansi driver, whose capability negotiation never completes
/// under some multiplexers and leaves a blank screen, so the dotnet driver is
/// asked for instead: it draws through System.Console and negotiates nothing.
/// Windows keeps the toolkit's native console driver. A driver the reader
/// names overrides both.
/// </summary>
public static class TerminalDriverChoice
{
    private const string ConsoleDriver = "dotnet";

    /// <returns>The driver's name, or null to let the toolkit choose.</returns>
    public static string? For(string? requested, bool onWindows) => requested switch
    {
        { Length: > 0 } => requested,
        _ when onWindows => null,
        _ => ConsoleDriver
    };
}
