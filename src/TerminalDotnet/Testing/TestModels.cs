namespace TerminalDotnet.Testing;

public sealed record TestCase(string FullyQualifiedName, string DisplayName, string ProjectPath)
{
    /// <summary>The namespace-qualified class the test belongs to, which identifies the
    /// suite it runs with. Two namespaces can hold a class of the same short name.</summary>
    public string TestClass
    {
        get
        {
            var methodSeparator = FullyQualifiedName.LastIndexOf('.');
            return methodSeparator < 0 ? FullyQualifiedName : FullyQualifiedName[..methodSeparator];
        }
    }

    /// <summary>The class name on its own, for display.</summary>
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

public sealed record TestRunSummary(int Passed, int Failed, int Skipped);

public sealed record TestRun(bool Passed, string Output, IReadOnlyList<TestResult> Results)
{
    public TestRun(bool passed, string output) : this(passed, output, [])
    {
    }

    public TestRunSummary Summary => new(
        CountOf(TestOutcome.Passed),
        CountOf(TestOutcome.Failed),
        CountOf(TestOutcome.Skipped));

    private int CountOf(TestOutcome outcome) => Results.Count(result => result.Outcome == outcome);
}

public interface ITestBackend
{
    Task<IReadOnlyList<TestCase>> DiscoverAsync(string target, CancellationToken cancellationToken = default);

    Task<TestRun> RunAsync(
        IReadOnlyCollection<TestCase> tests,
        CancellationToken cancellationToken = default);
}
