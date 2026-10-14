using System.ComponentModel;

namespace TerminalDotnet.Testing;

/// <summary>Stopping a command's process tree. A process that has already
/// exited answers the kill with an error, and that is the end the caller
/// asked for.</summary>
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
