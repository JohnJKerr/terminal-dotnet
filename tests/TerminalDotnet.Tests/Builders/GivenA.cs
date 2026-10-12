using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using TerminalDotnet.Terminal;
using TerminalDotnet.Testing;

namespace TerminalDotnet.Tests.Builders;

/// <summary>Where every arrangement a suite shares with another starts.</summary>
internal static class GivenA
{
    public static CommentSessionBuilder CommentSession() => new();

    public static TestExplorerSessionBuilder TestExplorer() => new();

    public static IssueSessionBuilder IssuePanel() => new();

    public static TestResultBuilder ResultFor(TestCase test) => new(test);

    public static (PreviewScrollView Preview, Code Source) PreviewWithLongSource()
    {
        var source = new Code
        {
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            CanFocus = false,
            Text = string.Join('\n', Enumerable.Range(1, 80).Select(line => $"line {line}"))
        };
        var preview = new PreviewScrollView(() => source)
        {
            Width = 40,
            Height = 10
        };
        preview.Add(source);
        preview.Layout();
        return (preview, source);
    }

    /// <summary>A discovered test, named the way discovery names it: the
    /// display name is the method with its underscores read as spaces.</summary>
    public static TestCase TestCase(string fullyQualifiedName, string projectPath = "Shop.Tests.csproj") => new(
        fullyQualifiedName,
        fullyQualifiedName[(fullyQualifiedName.LastIndexOf('.') + 1)..].Replace('_', ' '),
        projectPath);
}
