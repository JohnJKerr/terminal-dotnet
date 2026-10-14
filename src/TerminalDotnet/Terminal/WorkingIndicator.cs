using Terminal.Gui.App;

namespace TerminalDotnet.Terminal;

public interface IFrameClock
{
    void AddTimeout(TimeSpan interval, Func<bool> callback);
}

public sealed class ApplicationFrameClock(IApplication application) : IFrameClock
{
    public void AddTimeout(TimeSpan interval, Func<bool> callback) =>
        application.AddTimeout(interval, callback);
}

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
