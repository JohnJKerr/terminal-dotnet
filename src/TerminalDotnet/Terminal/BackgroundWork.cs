namespace TerminalDotnet.Terminal;

/// <summary>
/// Keeps the work the panels started off the terminal's thread, so quitting
/// can wait for it. Cancelling a run only asks the process tree to end; the
/// application has to stay alive until that work has actually settled.
/// </summary>
public sealed class BackgroundWork
{
    private readonly List<Task> running = [];

    public void Track(Task work)
    {
        lock (running)
        {
            // A session dispatches on every keystroke, so work that has
            // already ended is dropped rather than held until the reader quits.
            running.RemoveAll(tracked => tracked.IsCompleted);
            running.Add(work);
        }
    }

    public Task EndedAsync()
    {
        lock (running)
        {
            return Task.WhenAll(running.Select(Settled));
        }
    }

    /// <summary>Waiting on the way out reports how long the work took, not how
    /// it went: a run cancelled by quitting must not throw out of shutdown.
    /// </summary>
    private static Task Settled(Task work) =>
        work.ContinueWith(_ => { }, TaskScheduler.Default);
}
