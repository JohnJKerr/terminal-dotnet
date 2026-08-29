using System.Collections.ObjectModel;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
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
    private const int ContentInset = 1;
    private const int PanelWidth = 20;
    private const int WorkspaceX = ContentInset + PanelWidth + 1;

    private CancellationTokenSource? runCancellation;
    private IReadOnlyList<OutputLine> outputLines = [];
    private bool openSourceRequested;

    public void Run()
    {
        while (RunTerminal())
        {
            OpenSource();
        }
    }

    private bool RunTerminal()
    {
        openSourceRequested = false;
        using IApplication application = Application.Create();
        application.Init();

        using var window = new Window { Title = "terminal-dotnet" };
        var panels = Panels();
        var tests = Tests();
        var output = Output(tests);
        var shortcuts = Shortcuts();

        window.Add(panels, tests, output, shortcuts);
        application.Keyboard.KeyDown += (_, key) =>
            HandleKey(application, key, panels, tests, output);
        Render(tests, output);
        tests.SetFocus();

        application.Run(window);
        runCancellation?.Cancel();
        runCancellation?.Dispose();
        runCancellation = null;
        return openSourceRequested;
    }

    private static ListView Panels()
    {
        var panels = new ListView
        {
            Title = "Panels",
            X = ContentInset,
            Y = ContentInset,
            Width = PanelWidth,
            Height = Dim.Fill(2),
            ShowMarks = false,
            KeystrokeNavigator = null
        };
        panels.SetSource(new ObservableCollection<string>(["Tests"]));
        panels.SelectedItem = 0;
        return panels;
    }

    private static ListView Tests() => new()
    {
        Title = "Tests",
        X = WorkspaceX,
        Y = ContentInset,
        Width = Dim.Fill(ContentInset),
        Height = Dim.Percent(55),
        ShowMarks = false,
        KeystrokeNavigator = null
    };

    private ListView Output(ListView tests)
    {
        var output = new ListView
        {
            Title = "Execution Output",
            X = WorkspaceX,
            Y = Pos.Bottom(tests),
            Width = Dim.Fill(ContentInset),
            Height = Dim.Fill(2),
            ShowMarks = false,
            KeystrokeNavigator = null
        };
        output.RowRender += (_, args) => ColorOutputRow(output, args);
        return output;
    }

    private static Label Shortcuts() => new()
    {
        X = 1,
        Y = Pos.AnchorEnd(1),
        Width = Dim.Fill(1),
        Height = 1,
        Text = "Tab pane  ↑/k up  ↓/j down  Enter/r run  R rerun  F failures  c cancel  o source  q quit"
    };

    private void HandleKey(
        IApplication application,
        Key key,
        ListView panels,
        ListView tests,
        ListView output)
    {
        if (Is(key, KeyCode.Q) || Is(key, KeyCode.Esc))
        {
            key.Handled = true;
            application.RequestStop();
            return;
        }

        if (Is(key, KeyCode.O))
        {
            key.Handled = true;
            RequestOpenSource(application);
            return;
        }

        if (panels.HasFocus && Is(key, KeyCode.Enter))
        {
            key.Handled = true;
            tests.SetFocus();
            return;
        }

        if (output.HasFocus && ScrollOutput(key, output))
        {
            key.Handled = true;
            return;
        }

        var command = tests.HasFocus ? CommandFor(key) : null;
        if (command is null)
        {
            return;
        }

        key.Handled = true;
        HandleCommand(application, command, tests, output);
    }

    private void HandleCommand(
        IApplication application,
        TerminalCommand command,
        ListView tests,
        ListView output)
    {
        if (command is CancelRun)
        {
            runCancellation?.Cancel();
            return;
        }

        _ = DispatchAsync(application, command.ExplorerCommand!, tests, output);
    }

    private async Task DispatchAsync(
        IApplication application,
        ExplorerCommand command,
        ListView tests,
        ListView output)
    {
        if (!RunsTests(command))
        {
            await session.DispatchAsync(command);
            Render(tests, output);
            return;
        }

        runCancellation?.Dispose();
        runCancellation = new CancellationTokenSource();
        var run = session.DispatchAsync(command, runCancellation.Token);
        Render(tests, output);
        await run;
        application.Invoke(() => Render(tests, output));
    }

    private void RequestOpenSource(IApplication application)
    {
        if (editorLauncher is null || session.State.SourceContext is null)
        {
            return;
        }

        openSourceRequested = true;
        application.RequestStop();
    }

    private void OpenSource()
    {
        var source = session.State.SourceContext!;
        editorLauncher!.OpenAsync(
            source.Path,
            source.HighlightLine).GetAwaiter().GetResult();
    }

    private void Render(ListView tests, ListView output)
    {
        var snapshot = TestPanelSnapshot.From(session.State, target);
        tests.Title = $"Tests — {snapshot.Target}";
        tests.SetSource(new ObservableCollection<string>(snapshot.Tests.Select(TestRow)));
        outputLines = snapshot.OutputLines;
        output.SetSource(new ObservableCollection<string>(snapshot.OutputLines.Select(line => line.Text)));
        if (snapshot.Tests.Count > 0)
        {
            tests.SelectedItem = snapshot.SelectedIndex;
        }
    }

    private void ColorOutputRow(ListView output, ListViewRowEventArgs args)
    {
        if (args.Row >= outputLines.Count || output.IsSelectedOrMarked(args.Row))
        {
            return;
        }

        var foreground = outputLines[args.Row].Tone switch
        {
            OutputLineTone.Failure => Color.BrightRed,
            OutputLineTone.Success => Color.BrightGreen,
            OutputLineTone.Skipped => Color.BrightYellow,
            OutputLineTone.Status => Color.BrightCyan,
            _ => Color.None
        };
        if (foreground == Color.None)
        {
            return;
        }

        var background = output.GetAttributeForRole(VisualRole.Normal).Background;
        args.RowAttribute = new global::Terminal.Gui.Drawing.Attribute(foreground, background);
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

    private static bool ScrollOutput(Key key, ListView output)
    {
        if (Is(key, KeyCode.K))
        {
            output.MoveUp(false);
            return true;
        }

        if (Is(key, KeyCode.J))
        {
            output.MoveDown(false);
            return true;
        }

        return false;
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

        return null;
    }

    private static bool RunsTests(ExplorerCommand command) => command is
        ExplorerCommand.RunSelected or
        ExplorerCommand.RerunLast or
        ExplorerCommand.RerunFailed;

    private static bool Is(Key key, KeyCode keyCode) => key.NoShift.KeyCode == keyCode;

    private record TerminalCommand(ExplorerCommand? ExplorerCommand = null);

    private sealed record CancelRun : TerminalCommand;
}
