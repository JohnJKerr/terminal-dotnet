using TerminalDotnet.Testing;
using TerminalDotnet.Trust;
using Xunit;

namespace TerminalDotnet.Tests.Trust;

public sealed class WhenTrustingAFolder : IDisposable
{
    private readonly DirectoryInfo home = Directory.CreateTempSubdirectory("terminal-dotnet-trust-");

    private string Store => Path.Combine(home.FullName, "config", "terminal-dotnet", "trusted-folders");

    private string Repository => Path.Combine(home.FullName, "work", "shop");

    private string LaunchFolder => Path.Combine(Repository, "src", "App");

    [Fact]
    public async Task It_does_not_trust_a_folder_it_has_never_been_told_to_trust()
    {
        // Arrange
        var trust = new WorkspaceTrust(Store, new GitRoot(Repository));

        // Act
        var decision = await trust.CheckAsync(LaunchFolder);

        // Assert
        Assert.False(decision.Trusted);
    }

    [Fact]
    public async Task It_asks_about_the_repository_the_folder_belongs_to()
    {
        // Arrange
        var trust = new WorkspaceTrust(Store, new GitRoot(Repository));

        // Act
        var decision = await trust.CheckAsync(LaunchFolder);

        // Assert
        Assert.Equal(Repository, decision.Folder);
    }

    [Fact]
    public async Task It_asks_about_the_launch_folder_outside_a_repository()
    {
        // Arrange
        var trust = new WorkspaceTrust(Store, new GitRoot(null));

        // Act
        var decision = await trust.CheckAsync(LaunchFolder);

        // Assert
        Assert.Equal(LaunchFolder, decision.Folder);
    }

    [Fact]
    public async Task It_remembers_a_trusted_repository_the_next_time_it_starts()
    {
        // Arrange
        await new WorkspaceTrust(Store, new GitRoot(Repository)).TryTrustAsync(Repository);
        var restarted = new WorkspaceTrust(Store, new GitRoot(Repository));

        // Act
        var decision = await restarted.CheckAsync(LaunchFolder);

        // Assert
        Assert.True(decision.Trusted);
    }

    [Fact]
    public async Task It_keeps_trusting_a_repository_after_trusting_another()
    {
        // Arrange
        var other = Path.Combine(home.FullName, "work", "billing");
        await new WorkspaceTrust(Store, new GitRoot(Repository)).TryTrustAsync(Repository);
        await new WorkspaceTrust(Store, new GitRoot(other)).TryTrustAsync(other);

        // Act
        var decision = await new WorkspaceTrust(Store, new GitRoot(Repository)).CheckAsync(LaunchFolder);

        // Assert
        Assert.True(decision.Trusted);
    }

    [Fact]
    public async Task It_does_not_extend_trust_to_a_separate_repository_inside_a_trusted_folder()
    {
        // Arrange
        var work = Path.Combine(home.FullName, "work");
        await new WorkspaceTrust(Store, new GitRoot(work)).TryTrustAsync(work);
        var trust = new WorkspaceTrust(Store, new GitRoot(Repository));

        // Act
        var decision = await trust.CheckAsync(LaunchFolder);

        // Assert
        Assert.False(decision.Trusted);
    }

    [Fact]
    public async Task It_says_so_when_the_answer_cannot_be_remembered()
    {
        // Arrange
        var blocked = Path.Combine(home.FullName, "not-a-folder");
        await File.WriteAllTextAsync(blocked, "");
        var trust = new WorkspaceTrust(Path.Combine(blocked, "trusted-folders"), new GitRoot(Repository));

        // Act
        var remembered = await trust.TryTrustAsync(Repository);

        // Assert
        Assert.False(remembered);
    }

    [Fact]
    public async Task It_trusts_nothing_from_a_trust_file_too_large_to_have_been_written_by_it()
    {
        // Arrange
        Directory.CreateDirectory(Path.GetDirectoryName(Store)!);
        var line = Repository + Environment.NewLine;
        await File.WriteAllTextAsync(Store, string.Concat(Enumerable.Repeat(line, 2_000_000 / line.Length + 1)));
        var trust = new WorkspaceTrust(Store, new GitRoot(Repository));

        // Act
        var decision = await trust.CheckAsync(LaunchFolder);

        // Assert
        Assert.False(decision.Trusted);
    }

    [Fact]
    public void It_names_the_folder_it_asks_about()
    {
        // Act
        var question = TrustQuestion.For(Repository);

        // Assert
        Assert.Contains(Repository, question.Message);
    }

    public void Dispose() => home.Delete(recursive: true);

    /// <summary>Answers `git rev-parse --show-toplevel` with the given root, or
    /// as git does outside a repository when there is none.</summary>
    private sealed class GitRoot(string? root) : ICommandRunner
    {
        public Task<CommandResult> RunAsync(
            CommandRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(root is null
                ? new CommandResult(128, "", "fatal: not a git repository")
                : new CommandResult(0, root + "\n", ""));
    }
}
