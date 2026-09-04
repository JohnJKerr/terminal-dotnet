using System.Text.RegularExpressions;
using System.Xml.Linq;
using TerminalDotnet.Git;
using TerminalDotnet.Testing;

namespace TerminalDotnet.Files;

public sealed partial class FileSystemExplorerBackend(ICommandRunner commandRunner) : IFileExplorerBackend
{
    public async Task<IReadOnlyList<FileEntry>> DiscoverAsync(
        string target,
        CancellationToken cancellationToken = default)
    {
        var projectPaths = ProjectPaths(target);
        var workingDirectory = Path.GetDirectoryName(Path.GetFullPath(target))!;
        var gitStatuses = await GitStatusesAsync(workingDirectory, cancellationToken);
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
                    gitStatuses.GetValueOrDefault(Path.GetFullPath(path), FileGitStatus.Unchanged)));
            }

            entries.AddRange(DeletedEntries(projectPath, projectDirectory, gitStatuses));
        }

        return entries;
    }

    private async Task<IReadOnlyDictionary<string, FileGitStatus>> GitStatusesAsync(
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        var rootResult = await commandRunner.RunAsync(
            new CommandRequest("git", ["rev-parse", "--show-toplevel"], workingDirectory),
            cancellationToken);
        if (rootResult.ExitCode != 0)
        {
            return new Dictionary<string, FileGitStatus>();
        }

        var repositoryRoot = rootResult.StandardOutput.Trim();
        var result = await commandRunner.RunAsync(
            new CommandRequest(
                "git",
                ["status", "--porcelain=v1", "--untracked-files=all"],
                repositoryRoot),
            cancellationToken);
        if (result.ExitCode != 0)
        {
            return new Dictionary<string, FileGitStatus>();
        }

        return GitStatusOutput.EntriesFrom(result.StandardOutput)
            .ToDictionary(
                entry => Path.GetFullPath(entry.RelativePath, repositoryRoot),
                entry => StatusFrom(entry.Kind),
                StringComparer.Ordinal);
    }

    private static FileGitStatus StatusFrom(GitChangeKind kind) => kind switch
    {
        GitChangeKind.Added => FileGitStatus.New,
        GitChangeKind.Deleted => FileGitStatus.Deleted,
        _ => FileGitStatus.Modified
    };

    private static IEnumerable<FileEntry> DeletedEntries(
        string projectPath,
        string projectDirectory,
        IReadOnlyDictionary<string, FileGitStatus> gitStatuses) => gitStatuses
        .Where(status => status.Value == FileGitStatus.Deleted)
        .Select(status => status.Key)
        .Where(path => IsProjectSourceFile(path, projectDirectory))
        .OrderBy(path => path, StringComparer.Ordinal)
        .Select(path => new FileEntry(projectPath, "", path, FileGitStatus.Deleted));

    private static bool IsProjectSourceFile(string path, string projectDirectory) =>
        path.StartsWith(projectDirectory + Path.DirectorySeparatorChar, StringComparison.Ordinal) &&
        Path.GetExtension(path).Equals(".cs", StringComparison.OrdinalIgnoreCase) &&
        IsSourceFile(path);

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
