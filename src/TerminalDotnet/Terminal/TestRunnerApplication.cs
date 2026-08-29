using System.Collections.ObjectModel;
using Terminal.Gui.App;
using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using TerminalDotnet.Explorer;

namespace TerminalDotnet.Terminal;

public sealed class TestRunnerApplication(
    TestExplorerSession session,
    string target,
    EditorLauncher? editorLauncher = null)
{
    private CancellationTokenSource? runCancellation;

    public void Run()
    {
        using IApplication application = Application.Create();
        application.Init();

        using var window = new Window { Title = "TerminalDotnet" };
        var header = Header();
        var panels = Panels();
        var tests = Tests();
        var result = Result();
        var output = Output(result);
        var help = Help();

        window.Add(header, panels, tests, result, output, help);
        application.Keyboard.KeyDown += (_, key) => HandleKey(application, key, tests, result, output, header);
        Render(tests, result, output, header);
        tests.SetFocus();

        application.Run(window);
        runCancellation?.Cancel();
        runCancellation?.Dispose();
    }

    private static Label Header() => new()
    {
        X = 1, Y = 0, Width = Dim.Fill(1), Height = 1
    };

    private static FrameView Panels() => new()
    {
        Title = "Panels", X = 0, Y = 1, Width = 14, Height = Dim.Fill(2), Text = "\n  2 Tests"
    };

    private static ListView Tests() => new()
    {
        Title = "Tests", X = 14, Y = 1, Width = 36, Height = Dim.Fill(2),
        ShowMarks = false, KeystrokeNavigator = null
    };

    private static FrameView Result() => new()
    {
        Title = "Result", X = 50, Y = 1, Width = Dim.Fill(), Height = Dim.Percent(50)
    };

    private static FrameView Output(FrameView result) => new()
    {
        Title = "Output", X = 50, Y = Pos.Bottom(result), Width = Dim.Fill(), Height = Dim.Fill(2)
    };

    private static Label Help() => new()
    {
        X = 1, Y = Pos.AnchorEnd(1), Width = Dim.Fill(1), Height = 1,
        Text = "↑/k up  ↓/j down  Enter/r run  R rerun  F failures  c cancel  o source  q quit"
    };

    private void HandleKey(
        IApplication application,
        Key key,
        ListView tests,
        FrameView result,
        FrameView output,
        Label header)
    {
        var command = CommandFor(key);
        if (command is null)
        {
            if (Is(key, KeyCode.Q) || Is(key, KeyCode.Esc))
            {
                key.Handled = true;
                application.RequestStop();
            }

            return;
        }

        key.Handled = true;
        if (command is OpenSource)
        {
            _ = OpenSourceAsync(application);
            return;
        }

        if (command is CancelRun)
        {
            runCancellation?.Cancel();
            return;
        }

        _ = DispatchAsync(application, command.ExplorerCommand!, tests, result, output, header);
    }

    private async Task DispatchAsync(
        IApplication application,
        ExplorerCommand command,
        ListView tests,
        FrameView result,
        FrameView output,
        Label header)
    {
        var runsTests = command is ExplorerCommand.RunSelected or
            ExplorerCommand.RerunLast or
            ExplorerCommand.RerunFailed;
        if (!runsTests)
        {
            await session.DispatchAsync(command);
            Render(tests, result, output, header);
            return;
        }

        runCancellation?.Dispose();
        runCancellation = new CancellationTokenSource();
        var run = session.DispatchAsync(command, runCancellation.Token);
        Render(tests, result, output, header);
        await run;
        application.Invoke(() => Render(tests, result, output, header));
    }

    private async Task OpenSourceAsync(IApplication application)
    {
        if (editorLauncher is null || session.State.SourceContext is null)
        {
            return;
        }

        await editorLauncher.OpenAsync(
            session.State.SourceContext.Path,
            session.State.SourceContext.HighlightLine);
        application.Invoke(() => { });
    }

    private void Render(ListView tests, FrameView result, FrameView output, Label header)
    {
        var snapshot = TestPanelSnapshot.From(session.State, target);
        header.Text = $"sidecar │ Tests › {snapshot.Target}";
        tests.SetSource(new ObservableCollection<string>(snapshot.Tests.Select(TestRow)));
        if (snapshot.Tests.Count > 0)
        {
            tests.SelectedItem = snapshot.SelectedIndex;
        }

        result.Title = snapshot.Result.Title;
        result.Text = $"{snapshot.Result.Summary}\n\n{snapshot.Result.Details}";
        output.Text = snapshot.Output;
    }

    private static string TestRow(VisibleTestNode node)
    {
        var marker = node.Outcome switch
        {
            TestNodeOutcome.Running => "◌",
            TestNodeOutcome.Passed => "✓",
            TestNodeOutcome.Failed => "✗",
            _ when node.Kind == TestNodeKind.Test => "•",
            _ => "▼"
        };
        return $"{new string(' ', node.Depth * 2)}{marker} {node.Name}";
    }

    private static TerminalCommand? CommandFor(Key key)
    {
        if (Is(key, KeyCode.CursorUp) || Is(key, KeyCode.K))
        {
            return new TerminalCommand(new ExplorerCommand.MoveUp());
        }

        if (Is(key, KeyCode.CursorDown) || Is(key, KeyCode.J))
        {
            return new TerminalCommand(new ExplorerCommand.MoveDown());
        }

        if (Is(key, KeyCode.Enter) || Is(key, KeyCode.R) && !key.IsShift)
        {
            return new TerminalCommand(new ExplorerCommand.RunSelected());
        }

        if (Is(key, KeyCode.R) && key.IsShift)
        {
            return new TerminalCommand(new ExplorerCommand.RerunLast());
        }

        if (Is(key, KeyCode.F) && key.IsShift)
        {
            return new TerminalCommand(new ExplorerCommand.RerunFailed());
        }

        if (Is(key, KeyCode.C))
        {
            return new CancelRun();
        }

        return Is(key, KeyCode.O) ? new OpenSource() : null;
    }

    private static bool Is(Key key, KeyCode keyCode) => key.NoShift.KeyCode == keyCode;

    private record TerminalCommand(ExplorerCommand? ExplorerCommand = null);

    private sealed record OpenSource : TerminalCommand;

    private sealed record CancelRun : TerminalCommand;
}
