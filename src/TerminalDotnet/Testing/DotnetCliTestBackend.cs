using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace TerminalDotnet.Testing;

public sealed partial class DotnetCliTestBackend : ITestBackend
{
    private readonly ICommandRunner commandRunner;
    private readonly ITestResultStore resultStore;

    public DotnetCliTestBackend(ICommandRunner commandRunner, ITestResultStore? resultStore = null)
    {
        this.commandRunner = commandRunner;
        this.resultStore = resultStore ?? new TemporaryTrxResultStore();
    }

    public async Task<IReadOnlyList<TestCase>> DiscoverAsync(
        string target,
        CancellationToken cancellationToken = default)
    {
        var request = new CommandRequest(
            "dotnet",
            ["test", target, "--list-tests", "--nologo", "--tl:off"],
            Path.GetDirectoryName(Path.GetFullPath(target))!);
        var result = await commandRunner.RunAsync(request, cancellationToken);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Test discovery failed: {result.StandardError}");
        }

        var listingTests = false;
        var testTarget = target;
        var tests = new List<TestCase>();
        foreach (var line in result.StandardOutput.Split('\n'))
        {
            if (line.StartsWith("Test run for ", StringComparison.Ordinal))
            {
                testTarget = TestTarget(line) ?? target;
                listingTests = false;
                continue;
            }

            if (line.Contains("The following Tests are available:", StringComparison.Ordinal))
            {
                listingTests = true;
                continue;
            }

            if (!listingTests || !NamesATest(line))
            {
                continue;
            }

            tests.Add(DiscoveredTest(line.Trim(), testTarget));
        }

        return tests;
    }

    private static bool NamesATest(string line) =>
        !string.IsNullOrWhiteSpace(line) && char.IsWhiteSpace(line[0]);

    private static TestCase DiscoveredTest(string discoveredName, string target)
    {
        var fullyQualifiedName = FullyQualifiedNameFrom(discoveredName);
        return new TestCase(
            fullyQualifiedName,
            DisplayNameFrom(discoveredName, fullyQualifiedName),
            target);
    }

    private static string FullyQualifiedNameFrom(string discoveredName)
    {
        var parameterStart = discoveredName.IndexOf('(');
        return parameterStart < 0 ? discoveredName : discoveredName[..parameterStart].TrimEnd();
    }

    private static string DisplayNameFrom(string discoveredName, string fullyQualifiedName)
    {
        var methodSeparator = fullyQualifiedName.LastIndexOf('.');
        return discoveredName[(methodSeparator + 1)..].Replace('_', ' ');
    }

    private static string? TestTarget(string line)
    {
        const string prefix = "Test run for ";
        var framework = line.IndexOf(" (", prefix.Length, StringComparison.Ordinal);
        return framework < 0 ? null : ProjectTarget(line[prefix.Length..framework]);
    }

    private static string ProjectTarget(string assemblyPath)
    {
        var binSegment = $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}";
        var bin = assemblyPath.IndexOf(binSegment, StringComparison.OrdinalIgnoreCase);
        if (bin < 0)
        {
            return assemblyPath;
        }

        var projectDirectory = assemblyPath[..bin];
        var projectName = Path.GetFileNameWithoutExtension(assemblyPath);
        return Path.Combine(projectDirectory, $"{projectName}.csproj");
    }

    public async Task<TestRun> RunAsync(
        IReadOnlyCollection<TestCase> tests,
        CancellationToken cancellationToken = default)
    {
        if (tests.Count == 0)
        {
            throw new ArgumentException("At least one test is required.", nameof(tests));
        }

        var target = tests.First().ProjectPath;
        if (tests.Any(test => test.ProjectPath != target))
        {
            throw new ArgumentException("All tests in a run must have the same target.", nameof(tests));
        }

        var filter = string.Join(
            '|',
            tests.Select(test => $"FullyQualifiedName={FilterValue(test.FullyQualifiedName)}"));
        var resultPath = resultStore.CreatePath();
        var result = await commandRunner.RunAsync(
            new CommandRequest(
                "dotnet",
                [
                    "test",
                    target,
                    "--filter",
                    filter,
                    "--logger",
                    $"trx;LogFileName={resultPath}",
                    "--nologo",
                    "--tl:on"
                ],
                Path.GetDirectoryName(Path.GetFullPath(target))!),
            cancellationToken);

        var output = string.IsNullOrWhiteSpace(result.StandardError)
            ? result.StandardOutput
            : $"{result.StandardOutput}{Environment.NewLine}{result.StandardError}";
        var recorded = await RecordedResultsAsync(resultPath, tests, cancellationToken);
        return new TestRun(result.ExitCode == 0, output.Trim(), recorded.Results)
        {
            Diagnostic = recorded.Diagnostic
        };
    }

    /// <summary>A test's name comes from the project being read, and the filter
    /// language gives meaning to some of its characters, so a name holding them
    /// would widen or break the filter. They are escaped as the filter syntax
    /// asks, and a generic type's comma is written the way VSTest expects.
    /// </summary>
    private static string FilterValue(string fullyQualifiedName) =>
        string.Concat(fullyQualifiedName.Select(EscapedFilterCharacter));

    private static string EscapedFilterCharacter(char character) => character switch
    {
        '\\' or '(' or ')' or '&' or '|' or '=' or '!' or '~' => $"\\{character}",
        ',' => "%2C",
        _ => character.ToString()
    };

    private sealed record RecordedResults(IReadOnlyList<TestResult> Results, string? Diagnostic);

    /// <summary>The run's outcomes come from the results file, so a file that
    /// is missing or unreadable is reported as such rather than swallowed:
    /// nothing else can tell an unrun test from a passing one.</summary>
    private async Task<RecordedResults> RecordedResultsAsync(
        string resultPath,
        IReadOnlyCollection<TestCase> tests,
        CancellationToken cancellationToken)
    {
        try
        {
            var trx = await resultStore.ReadAsync(resultPath, cancellationToken);
            return new RecordedResults(ParseResults(trx, tests), null);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new RecordedResults([], $"Could not read the test results: {exception.Message}");
        }
    }

    private static IReadOnlyList<TestResult> ParseResults(
        string trx,
        IReadOnlyCollection<TestCase> requestedTests)
    {
        var document = XDocument.Parse(trx);
        var definitions = document
            .Descendants()
            .Where(element => element.Name.LocalName == "UnitTest")
            .ToDictionary(
                element => (string)element.Attribute("id")!,
                element => element.Descendants().Single(child => child.Name.LocalName == "TestMethod"));

        var requestedByName = requestedTests
            .GroupBy(test => test.FullyQualifiedName, StringComparer.Ordinal)
            .ToDictionary(name => name.Key, name => name.ToArray(), StringComparer.Ordinal);

        return document
            .Descendants()
            .Where(element => element.Name.LocalName == "UnitTestResult")
            .Select(result => ToTestResult(result, definitions, requestedByName))
            .OfType<TestResult>()
            .ToArray();
    }

    private static TestResult? ToTestResult(
        XElement result,
        IReadOnlyDictionary<string, XElement> definitions,
        IReadOnlyDictionary<string, TestCase[]> requestedByName)
    {
        var testId = (string?)result.Attribute("testId");
        if (testId is null || !definitions.TryGetValue(testId, out var definition))
        {
            return null;
        }

        var fullyQualifiedName = $"{definition.Attribute("className")?.Value}.{definition.Attribute("name")?.Value}";
        if (!requestedByName.TryGetValue(fullyQualifiedName, out var candidates))
        {
            return null;
        }

        var resultDisplayName = result.Attribute("testName")?.Value.Replace('_', ' ');
        var test = candidates.FirstOrDefault(candidate =>
                resultDisplayName?.EndsWith(candidate.DisplayName, StringComparison.Ordinal) == true)
            ?? candidates[0];

        var stackTrace = result.Descendants().SingleOrDefault(element => element.Name.LocalName == "StackTrace")?.Value;
        var source = stackTrace is null ? null : SourceLocation().Match(stackTrace);
        var outcome = result.Attribute("outcome")?.Value switch
        {
            "Passed" => TestOutcome.Passed,
            "NotExecuted" => TestOutcome.Skipped,
            _ => TestOutcome.Failed
        };

        return new TestResult(
            test,
            outcome,
            TimeSpan.TryParse(result.Attribute("duration")?.Value, out var duration) ? duration : TimeSpan.Zero,
            result.Descendants().SingleOrDefault(element => element.Name.LocalName == "Message")?.Value,
            stackTrace,
            source?.Success == true ? source.Groups["file"].Value : null,
            source?.Success == true ? int.Parse(source.Groups["line"].Value) : null,
            result.Descendants().SingleOrDefault(element => element.Name.LocalName == "StdOut")?.Value);
    }

    [GeneratedRegex(@" in (?<file>.+):line (?<line>\d+)")]
    private static partial Regex SourceLocation();
}
