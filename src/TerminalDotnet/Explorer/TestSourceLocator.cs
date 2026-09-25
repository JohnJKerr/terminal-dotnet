using System.Text.RegularExpressions;
using TerminalDotnet.Files;
using TerminalDotnet.Testing;

namespace TerminalDotnet.Explorer;

public sealed record SourceLocation(string Path, int HighlightLine);

public interface ITestSourceLocator
{
    Task<SourceLocation?> LocateAsync(
        TestCase test,
        CancellationToken cancellationToken = default);
}

public sealed class FileTestSourceLocator : ITestSourceLocator
{
    /// <summary>Scans the project's sources, which takes long enough that it
    /// is kept off whichever thread asked.</summary>
    public Task<SourceLocation?> LocateAsync(
        TestCase test,
        CancellationToken cancellationToken = default) =>
        Task.Run(() => Located(test, cancellationToken), cancellationToken);

    private static SourceLocation? Located(TestCase test, CancellationToken cancellationToken)
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

        var classDeclaration = new Regex($@"\bclass\s+{Regex.Escape(parts[^2])}\b");
        var methodDeclaration = new Regex($@"\b{Regex.Escape(parts[^1])}\s*\(");
        return SourceFiles(projectDirectory)
            .Select(path =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return LocationIn(path, classDeclaration, methodDeclaration);
            })
            .FirstOrDefault(location => location is not null);
    }

    /// <summary>The method is looked for below its class, so a method of the
    /// same name in an earlier class of the file is not taken for it.</summary>
    private static SourceLocation? LocationIn(string path, Regex classDeclaration, Regex methodDeclaration)
    {
        if (FileText.ReadLinesWithin(path) is not { } lines)
        {
            return null;
        }

        var classLine = LineMatching(lines, classDeclaration);
        var methodLine = classLine < 0 ? -1 : LineMatching(lines, methodDeclaration, classLine);
        return methodLine < 0 ? null : new SourceLocation(path, methodLine + 1);
    }

    private static IEnumerable<string> SourceFiles(string projectDirectory) =>
        SourceTree.FilesUnder(projectDirectory, "*.cs")
            .OrderBy(path => path, StringComparer.Ordinal);

    private static int LineMatching(IReadOnlyList<string> lines, Regex pattern, int startIndex = 0)
    {
        for (var index = startIndex; index < lines.Count; index++)
        {
            if (pattern.IsMatch(lines[index]))
            {
                return index;
            }
        }

        return -1;
    }
}
