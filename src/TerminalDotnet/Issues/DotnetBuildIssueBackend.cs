using System.Text.RegularExpressions;
using TerminalDotnet.Testing;

namespace TerminalDotnet.Issues;

public sealed partial class DotnetBuildIssueBackend(ICommandRunner runner) : IIssueBackend
{
    public async Task<IReadOnlyList<CompilationIssue>> DiscoverAsync(
        string target,
        CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(target))!;
        var result = await runner.RunAsync(new CommandRequest(
            "dotnet",
            ["build", target, "--nologo", "--tl:off", "--no-incremental"],
            directory), cancellationToken);
        return [.. Lines(result.StandardOutput, result.StandardError)
            .Select(line => Parsed(line, directory))
            .OfType<CompilationIssue>()
            .Distinct()];
    }

    private static IEnumerable<string> Lines(params string[] output) => output
        .SelectMany(text => text.Split('\n', StringSplitOptions.RemoveEmptyEntries));

    private static CompilationIssue? Parsed(string line, string directory)
    {
        var match = IssueLine().Match(line.Trim());
        if (!match.Success)
        {
            return null;
        }

        var path = Path.GetFullPath(match.Groups["path"].Value, directory);
        return new CompilationIssue(
            path,
            Path.GetRelativePath(directory, path),
            int.Parse(match.Groups["line"].Value),
            int.Parse(match.Groups["column"].Value),
            match.Groups["code"].Value,
            match.Groups["message"].Value.Trim(),
            match.Groups["severity"].Value == "error" ? IssueSeverity.Error : IssueSeverity.Warning);
    }

    [GeneratedRegex(@"^(?<path>.+)\((?<line>\d+),(?<column>\d+)\): (?<severity>error|warning) (?<code>[^: ]+): (?<message>.*?)(?: \[[^\]]+\])?$")]
    private static partial Regex IssueLine();
}
