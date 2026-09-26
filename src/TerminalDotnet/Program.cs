using TerminalDotnet.Changes;
using TerminalDotnet.Comments;
using TerminalDotnet.Explorer;
using TerminalDotnet.Files;
using TerminalDotnet.Flags;
using TerminalDotnet.Issues;
using TerminalDotnet.Terminal;
using TerminalDotnet.Testing;
using TerminalDotnet.Trust;

var launchTarget = LaunchTarget.From(CandidatesIn(Environment.CurrentDirectory));
if (launchTarget is not LaunchTarget.Found { Path: var target })
{
    Console.Error.WriteLine(NoTargetMessage(launchTarget));
    return 1;
}

var launchFolder = Path.GetDirectoryName(Path.GetFullPath(target))!;
var commandRunner = new ProcessCommandRunner();

// The panels build the solution as soon as they start, and building runs code
// the solution defines, so nothing starts until the folder is trusted.
var trust = new WorkspaceTrust(WorkspaceTrust.DefaultStorePath(), commandRunner);
var decision = await trust.CheckAsync(launchFolder);
var remembered = true;
if (!decision.Trusted)
{
    if (!TrustPrompt.Ask(TrustQuestion.For(decision.Folder)))
    {
        Console.Error.WriteLine(TrustQuestion.Declined(decision.Folder));
        return 1;
    }

    remembered = await trust.TryTrustAsync(decision.Folder);
}

var clipboard = new CommandClipboard(commandRunner, launchFolder);
var folderBackend = new LaunchFolderBackend(commandRunner);
var changesetBackend = new GitChangesetBackend(commandRunner);
var fileSession = new FileExplorerSession(new FileSystemExplorerBackend(commandRunner));
var folderSession = new FileExplorerSession(folderBackend, FileGrouping.Folder);
var changesetSession = new ChangesetSession(changesetBackend);
var commentSession = new CommentSession(clipboard, new FileCommentStore());
var issueSession = new IssueSession(
    new DotnetBuildIssueBackend(commandRunner),
    clipboard,
    new FileFlagBackend(folderBackend));
var session = new TestExplorerSession(
    new DotnetCliTestBackend(commandRunner, new TemporaryTrxResultStore()),
    new FileTestSourceLocator(),
    new ChangesetUpdatedSourceProvider(changesetBackend));

var editor = EditorLauncher.Configured(
    Environment.GetEnvironmentVariable("VISUAL"),
    Environment.GetEnvironmentVariable("EDITOR"));
var editorLauncher = new EditorLauncher(editor, commandRunner);
using var workspaceWatcher = new FileSystemWorkspaceWatcher();
new TestRunnerApplication(
    new PanelSessions(session, fileSession, folderSession, changesetSession, commentSession, issueSession),
    target,
    editorLauncher,
    workspaceWatcher).Run();
if (!remembered)
{
    Console.Error.WriteLine(TrustQuestion.NotRemembered(decision.Folder, trust.StorePath));
}

return 0;

// Windows matches a three-letter extension pattern against longer ones too,
// so *.sln also finds the .slnx files.
static IReadOnlyList<string> CandidatesIn(string directory) => LaunchTarget.SearchPatterns
    .SelectMany(pattern => Directory.EnumerateFiles(directory, pattern))
    .Distinct(StringComparer.Ordinal)
    .ToArray();

static string NoTargetMessage(LaunchTarget target) => target is LaunchTarget.Ambiguous ambiguous
    ? $"More than one solution or project could be opened here: {string.Join(", ", ambiguous.Candidates.Select(Path.GetFileName))}."
    : "No .sln, .slnx, or .csproj file found in the current directory.";
