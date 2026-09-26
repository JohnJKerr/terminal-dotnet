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
    public static IssuePanelAction? ActionFor(Key key, CompilationIssue? issue, bool searchActive)
    {
        if (searchActive)
        {
            return null;
        }

        if (FilterFor(key) is { } filter)
        {
            return new IssuePanelAction.Dispatch(filter);
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

    private static IssueCommand? FilterFor(Key key) => !key.IsShift ? null : key.NoShift.KeyCode switch
    {
        KeyCode.X => new IssueCommand.ToggleErrors(),
        KeyCode.W => new IssueCommand.ToggleWarnings(),
        KeyCode.F => new IssueCommand.ToggleFlags(),
        _ => null
    };
}
