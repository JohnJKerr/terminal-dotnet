using TerminalDotnet.Explorer;
using TerminalDotnet.Issues;

namespace TerminalDotnet.Terminal;

public enum ToastTone
{
    Working,
    Succeeded,
    Failed,
    Waiting
}

/// <summary>A short message laid over the panels.</summary>
public sealed record Toast(string Text, ToastTone Tone)
{
    /// <summary>Work still underway stays up until it is done; anything else
    /// has been said once it has been read.</summary>
    public bool FadesAway => Tone != ToastTone.Working;
}

/// <summary>
/// A rebuild can be set off from any panel, so what it is doing and what it
/// found are told over the panels rather than in the small print of the two
/// panels it reloads, which the reader may not be looking at.
/// </summary>
public static class RebuildToast
{
    public static readonly TimeSpan FadeAfter = TimeSpan.FromSeconds(4);

    public static Toast Rebuilding(TimeSpan elapsed) =>
        new(ActivityMarker.Marking("Rebuilding...", elapsed), ToastTone.Working);

    public static Toast WaitingOnTheRun() =>
        new("Rebuild waits for the test run to finish", ToastTone.Waiting);

    /// <summary>A broken build leaves the tests as they were, so the toast says
    /// so rather than letting an old tree pass for a rediscovered one.</summary>
    public static Toast Finished(IssueState issues, ExplorerState tests)
    {
        var errors = issues.Issues.Count(issue => issue.Severity == IssueSeverity.Error);
        var warnings = issues.Issues.Count(issue => issue.Severity == IssueSeverity.Warning);
        var found = $"{Counted(errors, "error")}, {Counted(warnings, "warning")}";

        if (errors > 0)
        {
            return new($"Build failed — {found} · tests left as they were", ToastTone.Failed);
        }

        return tests.Status == ExplorerStatus.Failed
            ? new($"Built — {found} · test discovery failed", ToastTone.Failed)
            : new($"Built — {found} · tests rediscovered", ToastTone.Succeeded);
    }

    private static string Counted(int count, string noun) =>
        count == 1 ? $"1 {noun}" : $"{count} {noun}s";
}
