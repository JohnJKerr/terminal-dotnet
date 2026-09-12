namespace TerminalDotnet.Terminal;

/// <summary>
/// The marker that turns beside the status line while a run is in flight, so a
/// `dotnet test` that spends seconds building does not read as a frozen panel.
/// </summary>
public static class RunActivity
{
    public static readonly TimeSpan FrameDuration = TimeSpan.FromMilliseconds(120);

    private static readonly IReadOnlyList<string> Frames =
        ["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"];

    public static int FrameCount => Frames.Count;

    public static string MarkerAt(TimeSpan elapsed) =>
        Frames[(int)(Math.Max(0, elapsed.Ticks) / FrameDuration.Ticks % Frames.Count)];

    public static string Marking(string statusLine, TimeSpan elapsed) =>
        $"{MarkerAt(elapsed)} {statusLine}";
}
