using System.Collections.Concurrent;

namespace TerminalDotnet.Terminal;

/// <summary>
/// Brings the panels' work back to the terminal's own loop once whatever it
/// awaited has answered, so the sessions are only ever touched from one
/// thread and the views are only drawn from the thread that owns them.
///
/// Once the loop has ended nothing will take the work any more, and quitting
/// still waits for it to settle, so from then on it runs on the thread pool,
/// along with anything handed to the loop that the loop never got to.
/// </summary>
public sealed class TerminalLoopContext(Action<Action> invokeOnLoop) : SynchronizationContext
{
    private readonly ConcurrentDictionary<PostedWork, byte> pending = new();
    private volatile bool released;

    public void Release()
    {
        released = true;
        foreach (var work in pending.Keys)
        {
            ThreadPool.QueueUserWorkItem(_ => RunOnce(work));
        }
    }

    public override void Post(SendOrPostCallback callback, object? state)
    {
        // Registered before the check, so a release landing in between still
        // finds it waiting.
        var work = new PostedWork(callback, state);
        pending[work] = 0;
        if (released)
        {
            ThreadPool.QueueUserWorkItem(_ => RunOnce(work));
            return;
        }

        invokeOnLoop(() => RunOnce(work));
    }

    public override SynchronizationContext CreateCopy() => this;

    /// <summary>Work is claimed before it runs, so the loop and the release
    /// can both reach it without running it twice.</summary>
    private void RunOnce(PostedWork work)
    {
        if (pending.TryRemove(work, out _))
        {
            work.Run();
        }
    }

    private sealed class PostedWork(SendOrPostCallback callback, object? state)
    {
        public void Run() => callback(state);
    }
}
