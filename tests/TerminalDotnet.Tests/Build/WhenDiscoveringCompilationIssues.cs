using TerminalDotnet.Issues;
using TerminalDotnet.Testing;
using TerminalDotnet.Tests.Fakes;
using Xunit;

namespace TerminalDotnet.Tests.Build;

public sealed class WhenDiscoveringCompilationIssues
{
    [Fact]
    public async Task It_builds_the_target_with_restoring_and_without_using_incremental_outputs()
    {
        // Arrange
        var runner = RunnerWithIssues();
        var backend = new DotnetBuildIssueBackend(runner);

        // Act
        await backend.DiscoverAsync("/repo/Shop.slnx");

        // Assert
        Assert.Equal(
            ["build", "/repo/Shop.slnx", "--nologo", "--tl:off", "--no-incremental"],
            runner.LastRequest!.Arguments);
    }

    [Fact]
    public async Task It_reads_errors_and_warnings_from_build_output()
    {
        // Arrange
        var backend = new DotnetBuildIssueBackend(RunnerWithIssues());

        // Act
        var issues = await backend.DiscoverAsync("/repo/Shop.slnx");

        // Assert
        Assert.Equal([IssueSeverity.Error, IssueSeverity.Warning], issues.Select(issue => issue.Severity));
    }

    [Fact]
    public async Task It_reads_the_source_location_and_details()
    {
        // Arrange
        var backend = new DotnetBuildIssueBackend(RunnerWithIssues());

        // Act
        var issue = (await backend.DiscoverAsync("/repo/Shop.slnx")).First();

        // Assert
        Assert.Equal(
            $"src{Path.DirectorySeparatorChar}Cart.cs(12,9): error CS1002: ; expected",
            issue.Details);
    }

    private static RecordingCommandRunner RunnerWithIssues() => new(new CommandResult(1, """
        /repo/src/Cart.cs(12,9): error CS1002: ; expected [/repo/Shop.csproj]
        /repo/src/Price.cs(4,2): warning CS0168: Variable is declared but never used [/repo/Shop.csproj]
        """, ""));
}
