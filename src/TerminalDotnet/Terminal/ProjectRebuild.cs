using TerminalDotnet.Explorer;
using TerminalDotnet.Issues;

namespace TerminalDotnet.Terminal;

/// <summary>Whether asking for a rebuild started one, and if not, which
/// panel holds the reason.</summary>
public enum RebuildStart
{
    Started,

    /// <summary>The build or discovery is already out; the panels already
    /// say so.</summary>
    AlreadyRebuilding,

    /// <summary>A test run is using the build output a rebuild would replace.
    /// </summary>
    WaitingOnTheRun
}

/// <summary>
/// A rebuild answers two questions: does it compile, and what tests does it
/// hold. Discovery builds too, so the two run one after the other rather than
/// as two builds fighting over the same output, and a build that is broken
/// leaves the tests as they were: discovery could only fail on it, and would
/// take the last tree that worked away with it.
/// </summary>
public sealed class ProjectRebuild(IssueSession issues, TestExplorerSession tests, string target)
{
    /// <summary>Stands both panels up as working, so the reader sees the
    /// rebuild start. Refused while either is already busy: a second build
    /// would only queue behind the first, and a run would lose its output.
    /// </summary>
    public RebuildStart Start()
    {
        if (tests.State.Status == ExplorerStatus.Running)
        {
            return RebuildStart.WaitingOnTheRun;
        }

        if (issues.State.Loading || tests.State.Status == ExplorerStatus.Loading)
        {
            return RebuildStart.AlreadyRebuilding;
        }

        issues.Rebuilding();
        tests.Rediscovering();
        return RebuildStart.Started;
    }

    /// <summary>Reports each panel as it lands, because the issues are ready
    /// long before discovery has built again and listed the tests.</summary>
    public async Task RunAsync(Func<Task> onPanelRebuilt, CancellationToken cancellationToken = default)
    {
        await issues.LoadAsync(target, cancellationToken);
        await onPanelRebuilt();
        await RediscoverUnlessBrokenAsync(cancellationToken);
        await onPanelRebuilt();
    }

    private Task RediscoverUnlessBrokenAsync(CancellationToken cancellationToken)
    {
        if (issues.State.Issues.Any(issue => issue.Severity == IssueSeverity.Error))
        {
            tests.LeaveAsDiscovered();
            return Task.CompletedTask;
        }

        return tests.LoadAsync(target, cancellationToken);
    }
}
