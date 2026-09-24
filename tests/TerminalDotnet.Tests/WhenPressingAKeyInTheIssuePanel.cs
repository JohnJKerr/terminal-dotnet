using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using TerminalDotnet.Issues;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Issues;

public sealed class WhenPressingAKeyInTheIssuePanel
{
    [Fact]
    public void Pressing_capital_X_narrows_the_issues_to_the_errors()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.X | KeyCode.ShiftMask));

        // Assert
        Assert.Equal(new IssuePanelAction.Dispatch(new IssueCommand.ToggleErrors()), action);
    }

    [Fact]
    public void Pressing_capital_W_narrows_the_issues_to_the_warnings()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.W | KeyCode.ShiftMask));

        // Assert
        Assert.Equal(new IssuePanelAction.Dispatch(new IssueCommand.ToggleWarnings()), action);
    }

    [Fact]
    public void Pressing_capital_F_narrows_the_issues_to_the_flags()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.F | KeyCode.ShiftMask));

        // Assert
        Assert.Equal(new IssuePanelAction.Dispatch(new IssueCommand.ToggleFlags()), action);
    }

    [Fact]
    public void Pressing_1_narrows_nothing()
    {
        // Act
        var action = ActionFor(new Key(KeyCode.D1));

        // Assert
        Assert.Null(action);
    }

    [Fact]
    public void Pressing_p_leaves_the_preview_to_follow_the_selection()
    {
        // Arrange
        var issue = new CompilationIssue("/repo/Order.cs", "Order.cs", 3, 1, "CS0103", "missing", IssueSeverity.Error);

        // Act
        var action = IssuePanelKeyBindings.ActionFor(new Key(KeyCode.P), issue, searchActive: false);

        // Assert
        Assert.Null(action);
    }

    private static IssuePanelAction? ActionFor(Key key) =>
        IssuePanelKeyBindings.ActionFor(key, null, searchActive: false);
}
