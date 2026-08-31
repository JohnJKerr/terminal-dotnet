using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace TerminalDotnet.Files;

public sealed partial class FileSystemExplorerBackend : IFileExplorerBackend
{
    public async Task<IReadOnlyList<FileEntry>> DiscoverAsync(
        string target,
        CancellationToken cancellationToken = default)
    {
        var projectPaths = ProjectPaths(target);
        var entries = new List<FileEntry>();
        foreach (var projectPath in projectPaths)
        {
            var projectDirectory = Path.GetDirectoryName(projectPath)!;
            foreach (var path in Directory
                .EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
                .Where(IsSourceFile)
                .OrderBy(path => path, StringComparer.Ordinal))
            {
                var source = await File.ReadAllTextAsync(path, cancellationToken);
                var declaredNamespace = NamespaceDeclaration().Match(source);
                entries.Add(new FileEntry(
                    projectPath,
                    declaredNamespace.Success ? declaredNamespace.Groups[1].Value : "(global)",
                    path,
                    FileGitStatus.Unchanged));
            }
        }

        return entries;
    }

    private static IReadOnlyList<string> ProjectPaths(string target)
    {
        var fullTarget = Path.GetFullPath(target);
        if (Path.GetExtension(fullTarget).Equals(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            return [fullTarget];
        }

        var directory = Path.GetDirectoryName(fullTarget)!;
        if (Path.GetExtension(fullTarget).Equals(".slnx", StringComparison.OrdinalIgnoreCase))
        {
            return XDocument.Load(fullTarget)
                .Descendants("Project")
                .Select(project => project.Attribute("Path")?.Value)
                .Where(path => path is not null)
                .Select(path => Path.GetFullPath(path!, directory))
                .ToArray();
        }

        return File.ReadLines(fullTarget)
            .Select(line => SolutionProjectPath().Match(line))
            .Where(match => match.Success)
            .Select(match => Path.GetFullPath(match.Groups[1].Value, directory))
            .ToArray();
    }

    private static bool IsSourceFile(string path)
    {
        var segments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return !segments.Contains("bin", StringComparer.OrdinalIgnoreCase) &&
            !segments.Contains("obj", StringComparer.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"\bnamespace\s+([A-Za-z_][A-Za-z0-9_.]*)\s*[;{]")]
    private static partial Regex NamespaceDeclaration();

    [GeneratedRegex("Project\\([^)]*\\)\\s*=\\s*\"[^\"]+\",\\s*\"([^\"]+\\.csproj)\"")]
    private static partial Regex SolutionProjectPath();
}
