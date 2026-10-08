using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using TerminalDotnet.Issues;

namespace TerminalDotnet.Terminal;

public abstract record IssuePanelAction
{
    public sealed record Edit(string Path, int Line) : IssuePanelAction;
    public sealed record Copy : IssuePanelAction;
    public sealed record Dispatch(IssueCommand Command) : IssuePanelAction;
}

public static class IssuePanelKeyBindings
{
    private static readonly IssueFilter[] Filters = Enum.GetValues<IssueFilter>();

    public static IssuePanelAction? ActionFor(
        Key key,
        CompilationIssue? issue,
        bool searchActive,
        IssueFilter? activeFilter = null)
    {
        if (searchActive)
        {
            return null;
        }

        if (FilterKeyBindings.StepFor(key) is { } step)
        {
            return new IssuePanelAction.Dispatch(ToggleFor(activeFilter, step));
        }

        if (issue is null)
        {
            return null;
        }

        return key.NoShift.KeyCode switch
        {
            KeyCode.Enter or KeyCode.E when !key.IsShift => new IssuePanelAction.Edit(issue.Path, issue.Line),
            KeyCode.Y when !key.IsShift => new IssuePanelAction.Copy(),
            _ => null
        };
    }

    /// <summary>The toggle that takes a step along the filters. Stepping off
    /// either end toggles the filter in use, which turns it off and returns
    /// the panel to every issue.</summary>
    private static IssueCommand ToggleFor(IssueFilter? active, int step) =>
        (FilterRing.Stepped(Filters, active, step) ?? active) switch
        {
            IssueFilter.Warnings => new IssueCommand.ToggleWarnings(),
            IssueFilter.Flags => new IssueCommand.ToggleFlags(),
            _ => new IssueCommand.ToggleErrors()
        };
}
