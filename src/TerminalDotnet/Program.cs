using TerminalDotnet.Explorer;
using TerminalDotnet.Terminal;
using TerminalDotnet.Testing;

var target = FindTarget(Environment.CurrentDirectory);
if (target is null)
{
    Console.Error.WriteLine("No .sln, .slnx, or .csproj file found in the current directory.");
    return 1;
}

var session = new TestExplorerSession(
    new DotnetCliTestBackend(new ProcessCommandRunner(), new TemporaryTrxResultStore()),
    new FileSourceProvider());
var editor = Environment.GetEnvironmentVariable("EDITOR");
var editorLauncher = string.IsNullOrWhiteSpace(editor)
    ? null
    : new EditorLauncher(editor, new ProcessCommandRunner());
Task? activeRun = null;
CancellationTokenSource? runCancellation = null;
ExplorerState? renderedState = null;
try
{
    await session.LoadAsync(target);
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}

while (true)
{
    if (!ReferenceEquals(renderedState, session.State))
    {
        Render(session.State, target);
        renderedState = session.State;
    }

    if (activeRun is not null && activeRun.IsCompleted)
    {
        await activeRun;
        activeRun = null;
        runCancellation?.Dispose();
        runCancellation = null;
        continue;
    }

    if (activeRun is not null && !Console.KeyAvailable)
    {
        await Task.Delay(50);
        continue;
    }

    var key = Console.ReadKey(intercept: true);
    if (key.Key is ConsoleKey.Q || key.Key is ConsoleKey.Escape)
    {
        if (runCancellation is not null)
        {
            await runCancellation.CancelAsync();
            await activeRun!;
        }

        break;
    }

    if (key.KeyChar == 'c' && runCancellation is not null)
    {
        await runCancellation.CancelAsync();
        continue;
    }

    if (key.KeyChar == 'o' && editorLauncher is not null && session.State.SourceContext is not null)
    {
        await editorLauncher.OpenAsync(
            session.State.SourceContext.Path,
            session.State.SourceContext.HighlightLine);
        renderedState = null;
        continue;
    }

    ExplorerCommand? command = key switch
    {
        { Key: ConsoleKey.UpArrow } or { KeyChar: 'k' } => new ExplorerCommand.MoveUp(),
        { Key: ConsoleKey.DownArrow } or { KeyChar: 'j' } => new ExplorerCommand.MoveDown(),
        { KeyChar: 'r' } or { Key: ConsoleKey.Enter } => new ExplorerCommand.RunSelected(),
        { KeyChar: 'R' } => new ExplorerCommand.RerunLast(),
        { KeyChar: 'F' } => new ExplorerCommand.RerunFailed(),
        _ => null
    };

    if (command is not null)
    {
        if (command is ExplorerCommand.RunSelected or ExplorerCommand.RerunLast or ExplorerCommand.RerunFailed)
        {
            runCancellation = new CancellationTokenSource();
            activeRun = session.DispatchAsync(command, runCancellation.Token);
            continue;
        }

        await session.DispatchAsync(command);
    }
}

Console.Clear();
return 0;

static string? FindTarget(string directory)
{
    var candidates = Directory.EnumerateFiles(directory, "*.sln")
        .Concat(Directory.EnumerateFiles(directory, "*.slnx"))
        .Concat(Directory.EnumerateFiles(directory, "*.csproj"))
        .OrderBy(path => path, StringComparer.Ordinal)
        .ToArray();
    return candidates.Length == 1 ? candidates[0] : candidates.FirstOrDefault();
}

static void Render(ExplorerState state, string target)
{
    Console.Clear();
    Console.WriteLine(" TerminalDotnet — THROWAWAY PROTOTYPE");
    Console.WriteLine($" {Path.GetFileName(target)}");
    Console.WriteLine(new string('─', Math.Max(20, Math.Min(Console.WindowWidth - 1, 80))));

    for (var index = 0; index < state.VisibleNodes.Count; index++)
    {
        var node = state.VisibleNodes[index];
        var cursor = index == state.SelectedIndex ? ">" : " ";
        var marker = node.Outcome switch
        {
            TestNodeOutcome.Running => "◌",
            TestNodeOutcome.Passed => "✓",
            TestNodeOutcome.Failed => "✗",
            _ when node.Kind == TestNodeKind.Test => "•",
            _ => "▼"
        };
        Console.WriteLine($"{cursor} {new string(' ', node.Depth * 2)}{marker} {node.Name}");
    }

    Console.WriteLine();
    Console.WriteLine(new string('─', Math.Max(20, Math.Min(Console.WindowWidth - 1, 80))));
    Console.WriteLine(state.Message);
    var failure = state.LastRun?.Results.FirstOrDefault(result => result.Outcome == TestOutcome.Failed);
    if (failure is not null)
    {
        Console.WriteLine();
        Console.WriteLine($"✗ {failure.Test.DisplayName} ({failure.Duration.TotalMilliseconds:0} ms)");
        Console.WriteLine(failure.ErrorMessage);
        if (failure.SourceFile is not null)
        {
            Console.WriteLine($"{failure.SourceFile}:{failure.SourceLine}");
        }

        Console.WriteLine(failure.StackTrace);
    }

    if (state.SourceContext is not null)
    {
        Console.WriteLine();
        Console.WriteLine($"Source: {state.SourceContext.Path}");
        foreach (var (line, offset) in state.SourceContext.Lines.Select((line, offset) => (line, offset)))
        {
            var lineNumber = state.SourceContext.StartLine + offset;
            var cursor = lineNumber == state.SourceContext.HighlightLine ? ">" : " ";
            Console.WriteLine($"{cursor} {lineNumber,4} {line}");
        }
    }

    Console.WriteLine();
    Console.WriteLine(" ↑/k up  ↓/j down  r run  R rerun  F failures  c cancel  o source  q quit");
}
