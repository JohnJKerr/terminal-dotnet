using System.Text.RegularExpressions;
using System.Xml.Linq;
using TerminalDotnet.Testing;

namespace TerminalDotnet.Files;

public sealed partial class FileSystemExplorerBackend(ICommandRunner commandRunner) : IFileExplorerBackend
{
    private readonly GitFileListing listing = new(commandRunner);

    public async Task<IReadOnlyList<FileEntry>> DiscoverAsync(
        string target,
        CancellationToken cancellationToken = default)
    {
        var workingDirectory = Path.GetDirectoryName(Path.GetFullPath(target))!;
        var root = await listing.RootAsync(workingDirectory, cancellationToken);
        var gitStatuses = await listing.StatusesAsync(root, cancellationToken);
        var entries = new List<FileEntry>();
        foreach (var projectPath in ProjectPaths(target))
        {
            entries.AddRange(await ProjectEntriesAsync(projectPath, gitStatuses, cancellationToken));
        }

        return entries;
    }

    private async Task<IReadOnlyList<FileEntry>> ProjectEntriesAsync(
        string projectPath,
        IReadOnlyDictionary<string, FileGitStatus> gitStatuses,
        CancellationToken cancellationToken)
    {
        var projectDirectory = Path.GetDirectoryName(projectPath)!;
        var paths = await listing.FilesUnderAsync(projectDirectory, cancellationToken);
        return
        [
            .. paths.Select(path => FileEntryFor(projectPath, path, gitStatuses)),
            .. DeletedEntries(projectPath, projectDirectory, gitStatuses)
        ];
    }

    private static FileEntry FileEntryFor(
        string projectPath,
        string path,
        IReadOnlyDictionary<string, FileGitStatus> gitStatuses) => new(
        projectPath,
        path,
        gitStatuses.GetValueOrDefault(Path.GetFullPath(path), FileGitStatus.Unchanged));

    private static IEnumerable<FileEntry> DeletedEntries(
        string projectPath,
        string projectDirectory,
        IReadOnlyDictionary<string, FileGitStatus> gitStatuses) => gitStatuses
        .Where(status => status.Value == FileGitStatus.Deleted)
        .Select(status => status.Key)
        .Where(path => GitFileListing.IsUnder(path, projectDirectory))
        .OrderBy(path => path, StringComparer.Ordinal)
        .Select(path => new FileEntry(projectPath, path, FileGitStatus.Deleted));

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
                .Select(path => ProjectPathFrom(path!, directory))
                .ToArray();
        }

        return File.ReadLines(fullTarget)
            .Select(line => SolutionProjectPath().Match(line))
            .Where(match => match.Success)
            .Select(match => ProjectPathFrom(match.Groups[1].Value, directory))
            .ToArray();
    }

    private static string ProjectPathFrom(string declaredPath, string solutionDirectory) =>
        Path.GetFullPath(
            declaredPath.Replace('\\', Path.DirectorySeparatorChar),
            solutionDirectory);

    [GeneratedRegex("Project\\([^)]*\\)\\s*=\\s*\"[^\"]+\",\\s*\"([^\"]+\\.csproj)\"")]
    private static partial Regex SolutionProjectPath();
}
