using System.Text.RegularExpressions;
using TerminalDotnet.Testing;

namespace TerminalDotnet.Explorer;

/// <summary>Where a test or its failure lives. Contents are read when a view asks
/// to show them, so a source file that has moved cannot hold up a run.</summary>
public sealed record SourceLocation(string Path, int HighlightLine);

public interface ITestSourceLocator
{
    Task<SourceLocation?> LocateAsync(
        TestCase test,
        CancellationToken cancellationToken = default);
}

public sealed class FileTestSourceLocator : ITestSourceLocator
{
    public async Task<SourceLocation?> LocateAsync(
        TestCase test,
        CancellationToken cancellationToken = default)
    {
        var projectDirectory = Path.GetDirectoryName(Path.GetFullPath(test.ProjectPath));
        if (projectDirectory is null || !Directory.Exists(projectDirectory))
        {
            return null;
        }

        var parts = test.FullyQualifiedName.Split('.');
        if (parts.Length < 2)
        {
            return null;
        }

        var className = parts[^2];
        var methodName = parts[^1];
        foreach (var path in SourceFiles(projectDirectory))
        {
            var lines = await File.ReadAllLinesAsync(path, cancellationToken);
            var classLine = LineMatching(lines, $@"\bclass\s+{Regex.Escape(className)}\b");
            if (classLine < 0)
            {
                continue;
            }

            var methodLine = LineMatching(
                lines,
                $@"\b{Regex.Escape(methodName)}\s*\(",
                classLine);
            if (methodLine >= 0)
            {
                return new SourceLocation(path, methodLine + 1);
            }
        }

        return null;
    }

    private static IEnumerable<string> SourceFiles(string projectDirectory) =>
        Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsBuildOutput(path, projectDirectory))
            .OrderBy(path => path, StringComparer.Ordinal);

    private static bool IsBuildOutput(string path, string projectDirectory)
    {
        var relative = Path.GetRelativePath(projectDirectory, path);
        var firstDirectory = relative.Split(Path.DirectorySeparatorChar)[0];
        return firstDirectory is "bin" or "obj";
    }

    private static int LineMatching(
        IReadOnlyList<string> lines,
        string pattern,
        int startIndex = 0)
    {
        for (var index = startIndex; index < lines.Count; index++)
        {
            if (Regex.IsMatch(lines[index], pattern))
            {
                return index;
            }
        }

        return -1;
    }
}
