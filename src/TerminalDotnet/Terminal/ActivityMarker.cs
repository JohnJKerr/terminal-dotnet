namespace TerminalDotnet.Terminal;

/// <summary>
/// The marker that turns while the explorer waits on something slow, so a panel
/// that has nothing to show yet reads as working rather than empty.
/// </summary>
public static class ActivityMarker
{
    public static readonly TimeSpan FrameDuration = TimeSpan.FromMilliseconds(120);

    private static readonly IReadOnlyList<string> Frames =
        ["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"];

    public static int FrameCount => Frames.Count;

    public static string MarkerAt(TimeSpan elapsed) =>
        Frames[(int)(Math.Max(0, elapsed.Ticks) / FrameDuration.Ticks % Frames.Count)];

    public static string Marking(string message, TimeSpan elapsed) =>
        $"{MarkerAt(elapsed)} {message}";
}
