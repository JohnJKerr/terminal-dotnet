namespace TerminalDotnet.Testing;

public sealed record TestCase(string FullyQualifiedName, string DisplayName, string ProjectPath)
{
    public string ClassName
    {
        get
        {
            var parts = FullyQualifiedName.Split('.');
            return parts.Length > 1 ? parts[^2] : FullyQualifiedName;
        }
    }
}

public enum TestOutcome
{
    Passed,
    Failed,
    Skipped
}

public sealed record TestResult(
    TestCase Test,
    TestOutcome Outcome,
    TimeSpan Duration,
    string? ErrorMessage,
    string? StackTrace,
    string? SourceFile,
    int? SourceLine,
    string? Output = null);

public sealed record TestRun(bool Passed, string Output, IReadOnlyList<TestResult> Results)
{
    public TestRun(bool passed, string output) : this(passed, output, [])
    {
    }
}

public interface ITestBackend
{
    Task<IReadOnlyList<TestCase>> DiscoverAsync(string target, CancellationToken cancellationToken = default);

    Task<TestRun> RunAsync(
        IReadOnlyCollection<TestCase> tests,
        CancellationToken cancellationToken = default);
}
