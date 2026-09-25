using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using TerminalDotnet.Comments;
using TerminalDotnet.Issues;
using TerminalDotnet.Terminal;
using TerminalDotnet.Tests.Builders;
using Xunit;

namespace TerminalDotnet.Tests.Issues;

/// <summary>The issues cannot answer without a build, so they are the one panel
/// left out of the reload an outside edit brings on. The reader asks instead,
/// from wherever they are standing rather than only from the Issues panel.
/// </summary>
public sealed class WhenRebuildingTheIssues
{
    [Fact]
    public void Ctrl_r_rebuilds_from_any_panel()
    {
        // Act
        var action = ShellKeyBindings.ActionFor(
            new Key(KeyCode.R).WithCtrl, searchFocused: false);

        // Assert
        Assert.Equal(new ShellAction.Refresh(), action);
    }

    [Fact]
    public void It_rebuilds_even_while_the_reader_is_typing_a_search()
    {
        // Act
        var action = ShellKeyBindings.ActionFor(
            new Key(KeyCode.R).WithCtrl, searchFocused: true);

        // Assert
        Assert.Equal(new ShellAction.Refresh(), action);
    }

    [Fact]
    public void A_bare_r_is_left_to_the_panel_the_reader_is_on()
    {
        // Act
        var action = ShellKeyBindings.ActionFor(
            new Key(KeyCode.R), searchFocused: false);

        // Assert
        Assert.Null(action);
    }

    [Fact]
    public void The_shortcut_line_offers_it_wherever_the_reader_is()
    {
        // Act
        var shortcuts = PanelShortcuts.For(
            PanelKind.Explorer, Files(), Changes(), Tests(), Comments());

        // Assert
        Assert.Contains("^R refresh", shortcuts);
    }

    [Fact]
    public void The_shortcut_line_still_offers_it_while_searching()
    {
        // Act
        var shortcuts = PanelShortcuts.For(
            PanelKind.Explorer, Files(), Changes(), Tests(), Comments(), searchFocused: true);

        // Assert
        Assert.Contains("^R refresh", shortcuts);
    }

    [Fact]
    public void The_command_list_says_what_it_does()
    {
        // Act
        var anywhere = CommandMenu.Sections().Single(section => section.Title == "Anywhere");

        // Assert
        Assert.Contains(anywhere.Entries, entry => entry.Keys == "Ctrl+R");
    }

    [Fact]
    public async Task It_keeps_the_issues_from_the_last_build_in_front_of_the_reader()
    {
        // Arrange
        var session = GivenA.IssuePanel().WithIssues(Unused, Assigned).Build();
        await session.LoadAsync("App.csproj");

        // Act
        session.Rebuilding();

        // Assert
        Assert.Equal(2, session.State.Issues.Count);
    }

    [Fact]
    public async Task It_keeps_the_row_the_reader_was_on()
    {
        // Arrange
        var session = GivenA.IssuePanel().WithIssues(Unused, Assigned).Build();
        await session.LoadAsync("App.csproj");
        await session.DispatchAsync(new IssueCommand.SelectIndex(1));

        // Act
        await session.LoadAsync("App.csproj");

        // Assert
        Assert.Equal(1, session.State.SelectedIndex);
    }

    private static TerminalDotnet.Files.FileExplorerState Files() => new([]);

    private static TerminalDotnet.Changes.ChangesetState Changes() => new([]);

    private static TerminalDotnet.Explorer.ExplorerState Tests() =>
        new(TerminalDotnet.Explorer.ExplorerStatus.Ready, [], 0, "");

    private static CommentsState Comments() => new([]);

    private static readonly CompilationIssue Unused =
        new("/repo/Order.cs", "Order.cs", 12, 3, "CS0168", "unused", IssueSeverity.Warning);

    private static readonly CompilationIssue Assigned =
        new("/repo/Basket.cs", "Basket.cs", 4, 1, "CS0219", "assigned", IssueSeverity.Warning);
}
