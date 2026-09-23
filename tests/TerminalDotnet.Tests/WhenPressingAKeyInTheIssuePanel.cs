using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using TerminalDotnet.Issues;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Issues;

public sealed class WhenPressingAKeyInTheIssuePanel
{
    [Fact]
    public void Pressing_3_narrows_the_issues_to_the_flags()
    {
        // Act
        var action = IssuePanelKeyBindings.ActionFor(new Key(KeyCode.D3), null, searchActive: false);

        // Assert
        Assert.Equal(new IssuePanelAction.Dispatch(new IssueCommand.ToggleFlags()), action);
    }
}
