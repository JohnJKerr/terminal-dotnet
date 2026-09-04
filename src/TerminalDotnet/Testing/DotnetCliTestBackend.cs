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
            tests.Select(test => $"FullyQualifiedName={test.FullyQualifiedName}"));
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
        var trx = await resultStore.ReadAsync(resultPath, cancellationToken);
        return new TestRun(result.ExitCode == 0, output.Trim(), ParseResults(trx, tests));
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

        return document
            .Descendants()
            .Where(element => element.Name.LocalName == "UnitTestResult")
            .Select(result => ToTestResult(result, definitions, requestedTests))
            .OfType<TestResult>()
            .ToArray();
    }

    private static TestResult? ToTestResult(
        XElement result,
        IReadOnlyDictionary<string, XElement> definitions,
        IReadOnlyCollection<TestCase> requestedTests)
    {
        var testId = (string?)result.Attribute("testId");
        if (testId is null || !definitions.TryGetValue(testId, out var definition))
        {
            return null;
        }

        var fullyQualifiedName = $"{definition.Attribute("className")?.Value}.{definition.Attribute("name")?.Value}";
        var resultDisplayName = result.Attribute("testName")?.Value.Replace('_', ' ');
        var test = requestedTests.FirstOrDefault(candidate =>
                candidate.FullyQualifiedName == fullyQualifiedName &&
                resultDisplayName?.EndsWith(candidate.DisplayName, StringComparison.Ordinal) == true)
            ?? requestedTests.FirstOrDefault(candidate => candidate.FullyQualifiedName == fullyQualifiedName);
        if (test is null)
        {
            return null;
        }

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
