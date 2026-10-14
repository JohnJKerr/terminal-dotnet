namespace TerminalDotnet.Testing;

/// <summary>What a run keeps of the command's output. The terminal logger
/// and a long suite both write far more than the panel can show, and the
/// whole of it was kept on the run and again on the status line.</summary>
public static class TestOutput
{
    public const int TailLength = 32 * 1024;

    public static string Tail(string output) =>
        output.Length <= TailLength ? output : output[^TailLength..];
}
