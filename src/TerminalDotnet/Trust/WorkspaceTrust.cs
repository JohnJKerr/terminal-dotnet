using TerminalDotnet.Files;
using TerminalDotnet.Git;
using TerminalDotnet.Testing;

namespace TerminalDotnet.Trust;

/// <summary>
/// The folders the reader trusts to build. Building a .NET solution runs code
/// the solution defines, so a folder is only built once the reader has said
/// they trust it, and the answer is remembered in a file of one folder per
/// line.
///
/// Trust belongs to a repository as a whole, so the question is about the
/// repository root, or about the launch folder outside git. Only an exact
/// match counts: trusting a folder says nothing about a separate repository
/// nested inside it.
/// </summary>
public sealed class WorkspaceTrust(string storePath, ICommandRunner commandRunner)
{
    /// <summary>Far more than anyone's list of trusted folders. A larger file
    /// was not written by the app, and trust is never taken from a file it
    /// cannot read whole.</summary>
    private const long MaxStoreBytes = 1024 * 1024;

    private static readonly StringComparer FolderComparer = OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    public string StorePath => storePath;

    public static string DefaultStorePath() => Path.Combine(
        Environment.GetFolderPath(
            Environment.SpecialFolder.ApplicationData,
            Environment.SpecialFolderOption.DoNotVerify),
        "terminal-dotnet",
        "trusted-folders");

    public async Task<TrustDecision> CheckAsync(
        string launchFolder,
        CancellationToken cancellationToken = default)
    {
        var folder = await FolderToTrustAsync(launchFolder, cancellationToken);
        return new TrustDecision(folder, TrustedFolders().Contains(folder, FolderComparer));
    }

    /// <returns>Whether the answer was remembered. A folder trusted but not
    /// remembered is still trusted for this launch, and asked about again on
    /// the next.</returns>
    public async Task<bool> TryTrustAsync(string folder, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> trusted = [.. TrustedFolders(), Normalized(folder)];
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(storePath))!);
            await FileReplacement.WriteAsync(storePath, StoreText(trusted), cancellationToken);
            return true;
        }
        catch (Exception exception) when (exception is IOException
            or UnauthorizedAccessException
            or NotSupportedException
            or ArgumentException)
        {
            return false;
        }
    }

    private async Task<string> FolderToTrustAsync(string launchFolder, CancellationToken cancellationToken)
    {
        var root = await commandRunner.RunAsync(
            GitRequest.For(["rev-parse", "--show-toplevel"], launchFolder),
            cancellationToken);
        return Normalized(root.ExitCode == 0 && root.StandardOutput.Trim() is { Length: > 0 } top
            ? top
            : launchFolder);
    }

    private IReadOnlyList<string> TrustedFolders()
    {
        try
        {
            return [.. (FileText.ReadLinesWithin(storePath, MaxStoreBytes) ?? [])
                .Where(line => line.Length > 0)
                .Select(Normalized)];
        }
        catch (Exception exception) when (exception is IOException
            or UnauthorizedAccessException
            or ArgumentException)
        {
            return [];
        }
    }

    private static string StoreText(IReadOnlyList<string> folders) => string.Concat(folders
        .Distinct(FolderComparer)
        .Select(folder => folder + Environment.NewLine));

    private static string Normalized(string folder) =>
        Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder));
}
