namespace TerminalDotnet.Testing;

public sealed record TestCase(string FullyQualifiedName, string DisplayName, string ProjectPath);

public sealed record TestRun(bool Passed, string Output);

public interface ITestBackend
{
    Task<IReadOnlyList<TestCase>> DiscoverAsync(string target, CancellationToken cancellationToken = default);

    Task<TestRun> RunAsync(
        IReadOnlyCollection<TestCase> tests,
        CancellationToken cancellationToken = default);
}
