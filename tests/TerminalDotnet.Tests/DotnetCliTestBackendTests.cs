using TerminalDotnet.Testing;
using Xunit;

namespace TerminalDotnet.Tests;

public sealed class DotnetCliTestBackendTests
{
    [Fact]
    public async Task Discovery_issues_a_list_tests_command_and_returns_the_reported_tests()
    {
        // Arrange
        var runner = new InMemoryCommandRunner(new CommandResult(0, """
            Determining projects to restore...
            The following Tests are available:
                Shop.Tests.CartTests.Adds_item
                Shop.Tests.CartTests.Removes_item
            """, ""));
        var backend = new DotnetCliTestBackend(runner);

        // Act
        var tests = await backend.DiscoverAsync("/repo/Shop.sln");

        // Assert
        Assert.Equal("dotnet", runner.LastRequest!.FileName);
        Assert.Equal(
            ["test", "/repo/Shop.sln", "--list-tests", "--nologo", "--tl:off"],
            runner.LastRequest.Arguments);
        Assert.Equal(
        [
            "Shop.Tests.CartTests.Adds_item",
            "Shop.Tests.CartTests.Removes_item"
        ], tests.Select(test => test.FullyQualifiedName));
    }

    [Fact]
    public async Task Running_tests_issues_one_exact_filter_command()
    {
        // Arrange
        var runner = new InMemoryCommandRunner(new CommandResult(0, "2 tests passed", ""));
        var backend = new DotnetCliTestBackend(runner);

        // Act
        var run = await backend.RunAsync(
        [
            new TestCase("Shop.Tests.CartTests.Adds_item", "Adds item", "/repo/Shop.sln"),
            new TestCase("Shop.Tests.CartTests.Removes_item", "Removes item", "/repo/Shop.sln")
        ]);

        // Assert
        Assert.Equal(
        [
            "test",
            "/repo/Shop.sln",
            "--filter",
            "FullyQualifiedName=Shop.Tests.CartTests.Adds_item|FullyQualifiedName=Shop.Tests.CartTests.Removes_item",
            "--nologo",
            "--tl:off"
        ], runner.LastRequest!.Arguments);
        Assert.True(run.Passed);
        Assert.Equal("2 tests passed", run.Output);
    }

    private sealed class InMemoryCommandRunner(CommandResult result) : ICommandRunner
    {
        public CommandRequest? LastRequest { get; private set; }

        public Task<CommandResult> RunAsync(CommandRequest request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(result);
        }
    }
}
