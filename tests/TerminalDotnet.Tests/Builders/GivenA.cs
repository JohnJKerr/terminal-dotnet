using TerminalDotnet.Testing;

namespace TerminalDotnet.Tests.Builders;

/// <summary>Where every arrangement a suite shares with another starts.</summary>
internal static class GivenA
{
    public static CommentSessionBuilder CommentSession() => new();

    public static TestExplorerSessionBuilder TestExplorer() => new();

    /// <summary>A discovered test, named the way discovery names it: the
    /// display name is the method with its underscores read as spaces.</summary>
    public static TestCase TestCase(string fullyQualifiedName, string projectPath = "Shop.Tests.csproj") => new(
        fullyQualifiedName,
        fullyQualifiedName[(fullyQualifiedName.LastIndexOf('.') + 1)..].Replace('_', ' '),
        projectPath);
}
