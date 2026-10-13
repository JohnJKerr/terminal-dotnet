using TerminalDotnet.Issues;
using Xunit;

namespace TerminalDotnet.Tests.Build;

public sealed class WhenBuildingInTheBackground
{
    [Fact]
    public async Task It_starts_the_build_away_from_the_calling_thread()
    {
        // Arrange
        var caller = 0;
        var build = new RecordingBuild();

        // Act
        await Task.Run(() =>
        {
            caller = Environment.CurrentManagedThreadId;
            new BackgroundIssueBackend(build).DiscoverAsync("Shop.sln").GetAwaiter().GetResult();
        });

        // Assert
        Assert.NotEqual(caller, build.CalledOnThread);
    }

    private sealed class RecordingBuild : IIssueBackend
    {
        public int CalledOnThread { get; private set; }

        public Task<IReadOnlyList<CompilationIssue>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default)
        {
            CalledOnThread = Environment.CurrentManagedThreadId;
            return Task.FromResult<IReadOnlyList<CompilationIssue>>([]);
        }
    }
}
