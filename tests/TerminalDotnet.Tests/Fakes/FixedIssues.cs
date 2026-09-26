using TerminalDotnet.Issues;

namespace TerminalDotnet.Tests.Fakes;

/// <summary>A build that reports the same issues every time.</summary>
internal sealed class FixedIssues(params CompilationIssue[] issues) : IIssueBackend
{
    public Task<IReadOnlyList<CompilationIssue>> DiscoverAsync(
        string target,
        CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CompilationIssue>>(issues);
}
