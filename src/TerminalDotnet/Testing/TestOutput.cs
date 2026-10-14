namespace TerminalDotnet.Testing;

public static class TestOutput
{
    public const int TailLength = 32 * 1024;

    public static string Tail(string output) =>
        output.Length <= TailLength ? output : output[^TailLength..];
}
