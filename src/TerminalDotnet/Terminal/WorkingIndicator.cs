using Terminal.Gui.App;

namespace TerminalDotnet.Terminal;

/// <summary>The clock a working label turns on. A tick can ask it to come
/// back, and nothing else: redrawing the window is not its job.</summary>
public interface IFrameClock
{
    void AddTimeout(TimeSpan interval, Func<bool> callback);
}

public sealed class ApplicationFrameClock(IApplication application) : IFrameClock
{
    public void AddTimeout(TimeSpan interval, Func<bool> callback) =>
        application.AddTimeout(interval, callback);
}

/// <summary>
/// Turns a working label while a build or a run is out. The timeout already
/// woke the loop, and the label it updates is the only thing that changed, so
/// the tick does not redraw the whole window.
/// </summary>
public static class WorkingIndicator
{
    public static void Turn(
        IFrameClock clock,
        Func<bool> stillWorking,
        Action refreshLabel,
        Action? whenFinished = null) =>
        clock.AddTimeout(ActivityMarker.FrameDuration, () => Tick(stillWorking, refreshLabel, whenFinished));

    private static bool Tick(Func<bool> stillWorking, Action refreshLabel, Action? whenFinished)
    {
        if (stillWorking())
        {
            refreshLabel();
            return true;
        }

        whenFinished?.Invoke();
        return false;
    }
}
