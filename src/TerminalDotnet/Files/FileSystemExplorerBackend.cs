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
        var workingDirectory = Path.GetDirectoryName(Path.GetFullPath(target))!;
        var gitStatuses = await GitStatusesAsync(workingDirectory, cancellationToken);
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
        var paths = await ProjectFilesAsync(projectDirectory, cancellationToken);
        return
        [
            .. paths.Select(path => FileEntryFor(projectPath, path, gitStatuses)),
            .. DeletedEntries(projectPath, projectDirectory, gitStatuses)
        ];
    }

    private async Task<IReadOnlyList<string>> ProjectFilesAsync(
        string projectDirectory,
        CancellationToken cancellationToken)
    {
        var listing = await commandRunner.RunAsync(
            new CommandRequest(
                "git",
                ["ls-files", "--cached", "--others", "--exclude-standard"],
                projectDirectory),
            cancellationToken);

        return listing.ExitCode == 0
            ? TrackedFiles(listing.StandardOutput, projectDirectory)
            : FilesOnDisk(projectDirectory);
    }

    private static IReadOnlyList<string> TrackedFiles(string listing, string projectDirectory) => listing
        .ReplaceLineEndings("\n")
        .Split('\n', StringSplitOptions.RemoveEmptyEntries)
        .Select(path => Path.GetFullPath(path, projectDirectory))
        .Where(File.Exists)
        .Where(IsProjectFile)
        .OrderBy(path => path, StringComparer.Ordinal)
        .ToArray();

    private static IReadOnlyList<string> FilesOnDisk(string projectDirectory) => Directory
        .EnumerateFiles(projectDirectory, "*", SearchOption.AllDirectories)
        .Where(IsProjectFile)
        .OrderBy(path => path, StringComparer.Ordinal)
        .ToArray();

    private static FileEntry FileEntryFor(
        string projectPath,
        string path,
        IReadOnlyDictionary<string, FileGitStatus> gitStatuses) => new(
        projectPath,
        path,
        gitStatuses.GetValueOrDefault(Path.GetFullPath(path), FileGitStatus.Unchanged));

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
        .Where(path => IsUnder(path, projectDirectory))
        .OrderBy(path => path, StringComparer.Ordinal)
        .Select(path => new FileEntry(projectPath, path, FileGitStatus.Deleted));

    private static bool IsUnder(string path, string projectDirectory) =>
        path.StartsWith(projectDirectory + Path.DirectorySeparatorChar, StringComparison.Ordinal) &&
        IsProjectFile(path);

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

    private static bool IsProjectFile(string path)
    {
        var segments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return !segments.Contains("bin", StringComparer.OrdinalIgnoreCase) &&
            !segments.Contains("obj", StringComparer.OrdinalIgnoreCase);
    }

    [GeneratedRegex("Project\\([^)]*\\)\\s*=\\s*\"[^\"]+\",\\s*\"([^\"]+\\.csproj)\"")]
    private static partial Regex SolutionProjectPath();
}
