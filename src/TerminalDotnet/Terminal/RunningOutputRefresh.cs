namespace TerminalDotnet.Terminal;

public static class RunningOutputRefresh
{
    public static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(250);

    public static bool Replace(
        string showingTitle,
        string showingOutput,
        string latestTitle,
        string latestOutput,
        TimeSpan sinceShown) =>
        sinceShown >= Interval && (latestTitle != showingTitle || latestOutput != showingOutput);
}
