using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using TerminalDotnet.Issues;

namespace TerminalDotnet.Terminal;

public abstract record IssuePanelAction
{
    public sealed record Edit(string Path, int Line) : IssuePanelAction;
    public sealed record Preview(string Path, int Line) : IssuePanelAction;
    public sealed record Copy : IssuePanelAction;
    public sealed record Dispatch(IssueCommand Command) : IssuePanelAction;
}

public static class IssuePanelKeyBindings
{
    public static IssuePanelAction? ActionFor(Key key, CompilationIssue? issue, bool searchActive)
    {
        if (searchActive) return null;
        if (!key.IsShift && key.NoShift.KeyCode == KeyCode.D1) return new IssuePanelAction.Dispatch(new IssueCommand.ToggleErrors());
        if (!key.IsShift && key.NoShift.KeyCode == KeyCode.D2) return new IssuePanelAction.Dispatch(new IssueCommand.ToggleWarnings());
        if (issue is null) return null;
        return key.NoShift.KeyCode switch
        {
            KeyCode.Enter or KeyCode.E when !key.IsShift => new IssuePanelAction.Edit(issue.Path, issue.Line),
            KeyCode.P when !key.IsShift => new IssuePanelAction.Preview(issue.Path, issue.Line),
            KeyCode.Y when !key.IsShift => new IssuePanelAction.Copy(),
            _ => null
        };
    }
}
