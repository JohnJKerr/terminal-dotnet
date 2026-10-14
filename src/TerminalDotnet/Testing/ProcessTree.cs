using System.ComponentModel;

namespace TerminalDotnet.Testing;

public static class ProcessTree
{
    public static void Stop(Action kill)
    {
        try
        {
            kill();
        }
        catch (Exception exception) when (HasAlreadyExited(exception))
        {
        }
    }

    private static bool HasAlreadyExited(Exception exception) =>
        exception is InvalidOperationException or NotSupportedException or Win32Exception;
}
