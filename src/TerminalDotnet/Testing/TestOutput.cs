namespace TerminalDotnet.Testing;

public static class TestOutput
{
    public const int TailLength = 32 * 1024;

    public static readonly TimeSpan ReportInterval = TimeSpan.FromMilliseconds(250);

    public static string Tail(string output) =>
        output.Length <= TailLength ? output : output[^TailLength..];

    public static bool ReportDue(bool reported, TimeSpan sinceReported) =>
        !reported || sinceReported >= ReportInterval;
}
